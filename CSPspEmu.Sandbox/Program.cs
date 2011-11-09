﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection.Emit;
using CSPspEmu.Core.Cpu.Table;
using System.Runtime.InteropServices;
using CSPspEmu.Core.Cpu;
using CSPspEmu.Core;
using CSPspEmu.Core.Memory;
using System.IO;
using CSharpUtils.Extensions;
using CSPspEmu.Core.Tests;
using System.Diagnostics;
using CSPspEmu.Hle;
using CSPspEmu.Core.Cpu.Cpu;
using CSPspEmu.Core.Cpu.Assembler;
using System.Threading;
using System.Windows.Forms;
using CSPspEmu.Gui.Winforms;
using CSPspEmu.Hle.Modules.threadman;
using CSPspEmu.Hle.Modules.utils;
using CSPspEmu.Hle.Modules.display;
using CSPspEmu.Hle.Modules.loadexec;
using CSPspEmu.Hle.Modules.ctrl;
using CSPspEmu.Hle.Managers;
using CSPspEmu.Hle.Loader;
using CSharpUtils;
using CSharpUtils.Threading;
using System.Reflection;

namespace CSPspEmu.Sandbox
{
	unsafe class Program : IGuiExternalInterface
	{
		PspRtc PspRtc;
		PspDisplay PspDisplay;
		PspController PspController;
		PspMemory Memory;
		Processor Processor;
		PspMemoryStream MemoryStream;
		HleState HleState;
		TaskQueue CpuTaskQueue = new TaskQueue();
		Assembly HleModulesDll;
		AutoResetEvent PauseEvent;

		PspMemory IGuiExternalInterface.GetMemory()
		{
			return Memory;
		}

		public PspDisplay GetDisplay()
		{
			return PspDisplay;
		}

		public PspController GetController()
		{
			return PspController;
		}

		public void PauseResume(Action Action)
		{
			if (Paused)
			{
				Action();
			}
			else
			{
				Pause();
				try
				{
					Action();
				}
				finally
				{
					Resume();
				}
			}
		}

		public bool Paused
		{
			get
			{
				return (PauseEvent != null);
			}
		}

		public void Pause()
		{
			if (!Paused)
			{
				PauseEvent = new AutoResetEvent(false);

				CpuTaskQueue.EnqueueAndWaitStarted(() =>
				{
					while (!PauseEvent.WaitOne(TimeSpan.FromMilliseconds(10)))
					{
						if (!Processor.IsRunning) break;
					}
					PauseEvent = null;
				});
			}
		}

		public void Resume()
		{
			if (Paused)
			{
				PauseEvent.Set();
			}
		}

		public void LoadFile(String FileName)
		{
			CpuTaskQueue.Enqueue(() =>
			{
				_LoadFile(FileName);
			});
		}

		private void _LoadFile(String FileName)
		{
			if (HleModulesDll == null)
			{
				HleModulesDll = Assembly.LoadFile(Path.GetDirectoryName(typeof(Program).Assembly.Location) + @"\CSPspEmu.Hle.Modules.dll");
			}

			Processor.Reset();
			Memory.Reset();
			HleState = new HleState(Processor, PspRtc, PspDisplay, PspController);
			Processor.DebugSyscalls = true;
			//Processor.ShowInstructionStats = true;
			//Processor.TraceJIT = true;
			//Processor.CountInstructionsAndYield = false;

			var ElfStream = File.OpenRead(FileName);

			var Loader = new ElfPspLoader();

			Loader.LoadAllocateAndWrite(
				ElfStream,
				MemoryStream,
				HleState.MemoryManager.RootPartition
			);

			Loader.UpdateModuleImports(new PspMemoryStream(Memory), HleState.ModuleManager);

			var MainThread = HleState.ThreadManager.Create();
			MainThread.CpuThreadState.PC = Loader.InitInfo.PC;
			MainThread.CpuThreadState.GP = Loader.InitInfo.GP;
			MainThread.CpuThreadState.SP = (uint)(0x09000000 - 10000);
			MainThread.CpuThreadState.RA = (uint)0x08000000;
			MainThread.CurrentStatus = HleThread.Status.Ready;

			RegisterSyscalls();
		}

		void RegisterSyscalls()
		{
			new MipsAssembler(MemoryStream).Assemble(@"
			.code 0x08000000
				syscall 0x7777
				jr r31
				nop
			");

			Processor.RegisterNativeSyscall(0x7777, (Code, CpuThreadState) =>
			{
				var SleepThread = HleState.ThreadManager.Current;
				SleepThread.CurrentStatus = HleThread.Status.Waiting;
				SleepThread.CurrentWaitType = HleThread.WaitType.None;
				CpuThreadState.Yield();
			});

			var ThreadManForUser = HleState.ModuleManager.GetModule<ThreadManForUser>();

			Processor.RegisterNativeSyscall(0x206D, (Code, CpuThreadState) =>
			{
				HleState.ModuleManager.GetModuleDelegate<ThreadManForUser>("sceKernelCreateThread")(CpuThreadState);
			});

			Processor.RegisterNativeSyscall(0x206F, (Code, CpuThreadState) =>
			{
				HleState.ModuleManager.GetModuleDelegate<ThreadManForUser>("sceKernelStartThread")(CpuThreadState);
			});

			Processor.RegisterNativeSyscall(0x2071, (Code, CpuThreadState) =>
			{
				HleState.ModuleManager.GetModuleDelegate<ThreadManForUser>("sceKernelExitDeleteThread")(CpuThreadState);
			});

			Processor.RegisterNativeSyscall(0x20BF, (Code, CpuThreadState) =>
			{
				HleState.ModuleManager.GetModuleDelegate<UtilsForUser>("sceKernelUtilsMt19937Init")(CpuThreadState);
			});

			Processor.RegisterNativeSyscall(0x20C0, (Code, CpuThreadState) =>
			{
				HleState.ModuleManager.GetModuleDelegate<UtilsForUser>("sceKernelUtilsMt19937UInt")(CpuThreadState);
			});

			Processor.RegisterNativeSyscall(0x213A, (Code, CpuThreadState) =>
			{
				HleState.ModuleManager.GetModuleDelegate<sceDisplay>("sceDisplaySetMode")(CpuThreadState);
			});

			Processor.RegisterNativeSyscall(0x2147, (Code, CpuThreadState) =>
			{
				HleState.ModuleManager.GetModuleDelegate<sceDisplay>("sceDisplayWaitVblankStart")(CpuThreadState);
			});

			Processor.RegisterNativeSyscall(0x213f, (Code, CpuThreadState) =>
			{
				HleState.ModuleManager.GetModuleDelegate<sceDisplay>("sceDisplaySetFrameBuf")(CpuThreadState);
			});

			Processor.RegisterNativeSyscall(0x20eb, (Code, CpuThreadState) =>
			{
				HleState.ModuleManager.GetModuleDelegate<LoadExecForUser>("sceKernelExitGame")(CpuThreadState);
			});

			Processor.RegisterNativeSyscall(0x2150, (Code, CpuThreadState) =>
			{
				HleState.ModuleManager.GetModuleDelegate<sceCtrl>("sceCtrlPeekBufferPositive")(CpuThreadState);
			});
		}

		void Execute()
		{
			PspRtc = new PspRtc();
			PspDisplay = new PspDisplay(PspRtc);
			PspController = new PspController();
			Memory = new FastPspMemory();
			MemoryStream = new PspMemoryStream(Memory);
			Processor = new Processor(Memory);
			HleState = new HleState(Processor, PspRtc, PspDisplay, PspController);

			//LoadFile("../../../TestInput/minifire.elf");

			/*
			CpuTaskQueue.Enqueue(() =>
			{
				LoadFile("../../../TestInput/minifire.elf");
				//var ElfStream = File.OpenRead("../../../TestInput/HelloWorld.elf");
				//var ElfStream = File.OpenRead("../../../TestInput/HelloWorldPSP.elf");
				//var ElfStream = File.OpenRead("../../../TestInput/counter.elf");
				//var ElfStream = File.OpenRead(@"C:\projects\pspemu\pspautotests\tests\string\string.elf");
				//var ElfStream = File.OpenRead(@"C:\juegos\jpcsp2\demos\compilerPerf.elf");
				//var ElfStream = File.OpenRead(@"C:\juegos\jpcsp2\demos\fputest.elf");
				//var ElfStream = File.OpenRead(@"C:\projects\pspemu\pspautotests\demos\mytest.elf");
				//var ElfStream = File.OpenRead(@"C:\projects\pspemu\demos\dumper.elf");
				//var ElfStream = File.OpenRead(@"C:\projects\pspemu\pspautotests\tests\cpu\cpu\cpu.elf");
			});
			*/

			// Execution Thread.
			new Thread(() =>
			{
				while (Processor.IsRunning)
				{
					try
					{
						while (Processor.IsRunning)
						{
							CpuTaskQueue.HandleEnqueued();
							if (!Processor.IsRunning) break;
							HleState.PspRtc.Update();
							HleState.ThreadManager.StepNext();
						}
					}
					catch (Exception Exception)
					{
						ConsoleUtils.SaveRestoreConsoleState(() =>
						{
							Console.ForegroundColor = ConsoleColor.Red;

							Console.WriteLine(Exception);

							try
							{
								HleState.ThreadManager.Current.CpuThreadState.DumpRegisters();

								Console.WriteLine(
									"Last registered PC = 0x{0:X}, RA = 0x{1:X}",
									HleState.ThreadManager.Current.CpuThreadState.PC,
									HleState.ThreadManager.Current.CpuThreadState.RA
								);
							}
							catch
							{
							}
						});
						//throw (new Exception("Unhandled Exception " + Exception.ToString(), Exception));
						//throw (new Exception(Exception.InnerException.ToString(), Exception.InnerException));
					}
				}
			}).Start();

			// GUI Thread.
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new PspDisplayForm(this));
			try { Processor.IsRunning = false; } catch { }
		}


		/// <summary>
		/// 
		/// </summary>
		/// <see cref="http://en.wikipedia.org/wiki/Common_Intermediate_Language"/>
		/// <see cref="http://en.wikipedia.org/wiki/List_of_CIL_instructions"/>
		/// <param name="args"></param>
		[STAThread]
		static void Main(string[] args)
		{
			Console.SetWindowSize(160, 60);
			Console.SetBufferSize(160, 2000);
			new Program().Execute();
		}
	}
}