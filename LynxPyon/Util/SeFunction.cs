using System;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.Hooking;

namespace LynxPyon {
    public delegate void UpdatePartyDelegate(IntPtr hudAgent);

    public sealed class UpdateParty : SeFunction<UpdatePartyDelegate> {
        public UpdateParty(SigScanner sigScanner) : base(sigScanner, "40 ?? 48 83 ?? ?? 48 8B ?? 48 ?? ?? ?? 48 ?? ?? ?? ?? ?? ?? 83 ?? ?? ?? ?? ?? ?? 74 ?? 48") { }
    }

    public class SeFunction<T> where T : Delegate {
        public IntPtr Address;
        protected T? FuncDelegate;

        public SeFunction(SigScanner sigScanner, int offset) {
            Address = sigScanner.Module.BaseAddress + offset;
        }

        public SeFunction(SigScanner sigScanner, string signature, int offset = 0) {
            Address = sigScanner.ScanText(signature);
            if(Address != IntPtr.Zero) {
                Address += offset;
            }  
            var baseOffset = (ulong)Address.ToInt64() - (ulong)sigScanner.Module.BaseAddress.ToInt64();
        }

        public T? Delegate() {
            if(FuncDelegate != null) {
                return FuncDelegate;
            }

            if(Address != IntPtr.Zero) {
                FuncDelegate = Marshal.GetDelegateForFunctionPointer<T>(Address);
                return FuncDelegate;
            }

            return null;
        }

        public dynamic? Invoke(params dynamic[] parameters) {
            if(FuncDelegate != null) {
                return FuncDelegate.DynamicInvoke(parameters);
            }

            if(Address != IntPtr.Zero) {
                FuncDelegate = Marshal.GetDelegateForFunctionPointer<T>(Address);
                return FuncDelegate!.DynamicInvoke(parameters);
            } else {
                return null;
            }
        }

        public Hook<T>? CreateHook(T detour) {
            if(Address != IntPtr.Zero) {
                var hook = Hook<T>.FromAddress(Address, detour);
                hook.Enable();
                return hook;
            }

            return null;
        }
    }
}
