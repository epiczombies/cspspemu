﻿using System;
using System.Runtime.InteropServices;

namespace CSPspEmu.Core.Memory
{
    public sealed unsafe class FastPspMemory : PspMemory
    {
        public override bool HasFixedGlobalAddress => true;

        public override IntPtr FixedGlobalAddress => new IntPtr(_Base);

        //public const uint FastMemorySize = 0x0A000000;
        //public const uint FastMemoryMask = 0x1FFFFFFF;
        public const uint FastMemoryMask = 0x0FFFFFFF;

        public const uint FastMemorySize = FastMemoryMask + 1;


        public static byte* _Base = null;

        public FastPspMemory()
        {
            AllocMemoryOnce();
        }

        ~FastPspMemory()
        {
            Dispose();
            //FreeMemory();
        }

        private static bool AlreadyInitialized = false;

        private void AllocMemoryOnce()
        {
            if (!AlreadyInitialized)
            {
                AlreadyInitialized = true;

                _Base = (byte*) Marshal.AllocHGlobal((int) FastMemorySize).ToPointer();

                //Console.WriteLine("*****************************");
            }

            NullPtr = _Base;
            ScratchPadPtr = _Base + ScratchPadOffset;
            FrameBufferPtr = _Base + FrameBufferOffset;
            MainPtr = _Base + MainOffset;
        }

        public override uint PointerToPspAddressUnsafe(void* Pointer)
        {
            if (Pointer == null) return 0;
            return (uint) ((byte*) Pointer - _Base);
        }

        public override void* PspAddressToPointerUnsafe(uint _Address)
        {
            var Address = (_Address & FastPspMemory.FastMemoryMask);
            //Console.WriteLine("Base: 0x{0:X} ; Address: 0x{1:X}", (ulong)Base, Address);
            if (Address == 0) return null;
#if false
			if (_Base == null) throw(new InvalidProgramException("Base is null"));
#endif
            return _Base + Address;
        }

        public override void Dispose()
        {
        }
    }
}