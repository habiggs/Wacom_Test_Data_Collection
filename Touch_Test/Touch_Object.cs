using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using WacomMTDN;

namespace Touch_Test
{
    class Touch_Object
    {
        public CWacomMTConfig mWacomMTConfig = new CWacomMTConfig();

        // Manages receiving data from a subscribed Wacom Tablet
        public CWacomMTWindowClient mWacomMTWindowClient;
        private IntPtr mUserDataIntPtr = IntPtr.Zero;

        public ConcurrentQueue<WacomMTFingerCollection> fingerUpdates =
          new ConcurrentQueue<WacomMTFingerCollection>();



        public Touch_Object(WacomMTCallback Caller)
        {
            //Shutdown();
            Setup(Caller);
        }

        public void Setup(WacomMTCallback Caller)
        {

            mWacomMTConfig.Init();

            try
            {
                if (mWacomMTConfig.DeviceIDList.data.Length > 0)
                {
                    //Subscribe to Finger Touch Events
                    mWacomMTWindowClient = new CWacomMTFingerClient(WacomMTProcessingMode.WMTProcessingModeConsumer);

                    var callback = Caller;

                    WacomMTHitRect fullScreenHitRect = new WacomMTHitRect(0f, 0f, 1f, 1f);

                    mWacomMTWindowClient.RegisterHitRectClient(
                        mWacomMTConfig.DeviceIDList.data[0],
                        fullScreenHitRect,
                        ref callback,
                        mUserDataIntPtr);

                    Console.WriteLine("Touch_Object Successfully Connected to Tablet");
                }
            }
            catch
            {
                Console.WriteLine("Failed Touch_Object Initialization");
            }
        }

        public void Shutdown()
        {
            if (mWacomMTWindowClient != null &&
                mWacomMTWindowClient.IsRegisteredAsHitRectClient())
            {
                mWacomMTWindowClient.UnregisterHitRectClient();
            }

            if (mWacomMTConfig != null)
            {
                // Close the WacomMT connection.
                mWacomMTConfig.Quit();
                mWacomMTConfig = null;
            }

            if (mUserDataIntPtr != IntPtr.Zero)
            {
                CMemUtils.FreeUnmanagedString(mUserDataIntPtr);
                mUserDataIntPtr = IntPtr.Zero;
            }

            Console.WriteLine("Successfully Shutdown Touch_Object");
        }

        public UInt32 FingerCallback(IntPtr packet, IntPtr userData)
        {
            try
            {
                WacomMTFingerCollection fingerCollection =
                    CMemUtils.PtrToStructure<WacomMTFingerCollection>(packet);

                fingerUpdates.Enqueue(fingerCollection);
            }
            catch
            {
                Console.WriteLine("Failed to gather finger collection");
            }


            return 0;
        }

    }
}
