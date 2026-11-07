using System.Runtime.InteropServices;
namespace DeliverySystem.Native;
internal static class NativeMethods {
 [DllImport("deliverycalc", EntryPoint="delivery_surcharge_cents", CallingConvention=CallingConvention.Cdecl)]
 internal static extern int DeliverySurchargeCents(int distanceMeters,int rateCentsPerKm);
}
