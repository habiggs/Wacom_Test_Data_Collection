using System;
using System.Diagnostics;
using WacomMTDN;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Touch_Test
{
    class Program
    {
        public static Touch_Object My_Touch_Object;
        public static ConcurrentQueue<WacomMTFingerCollection> fingerUpdates =
            new ConcurrentQueue<WacomMTFingerCollection>();

        static float x_Save = 0;
        static float y_Save = 0;
        static float pinchSave = -1;
        static int frameNumSave = -1;
        public static void Main(string[] args)
        {
            My_Touch_Object = new Touch_Object(new WacomMTCallback(FingerCallback));

            //Keep App Running
            while (true)
            {
                
            }
        }

        public static UInt32 FingerCallback(IntPtr packet, IntPtr userData)
        {
            List<uint> Confident_Fingers = new List<uint>();
            try
            {
                WacomMTFingerCollection fingerCollection =
                    CMemUtils.PtrToStructure<WacomMTFingerCollection>(packet);

                if (fingerCollection.FrameNumber <= frameNumSave)
                {
                    x_Save = 0;
                    y_Save = 0;
                    pinchSave = -1;
                }

                Console.WriteLine("Frame Number" + fingerCollection.FrameNumber + ", Num Fingers:" + fingerCollection.FingerCount);

                for (int i = 0; i < fingerCollection.FingerCount; i++)
                {
                    var finger = fingerCollection.GetFingerByIndex((uint)i);

                    if (finger.Confidence)
                    {
                        Confident_Fingers.Add((uint)i);
                        Console.WriteLine("Finger num:" + i + ", X:" + finger.X + ", Y:" + finger.Y);
                    }
                }

                if (Confident_Fingers.Count > 1)
                {
                    float temp_X_pinch = ((1 - fingerCollection.GetFingerByIndex(Confident_Fingers[0]).X) - (1 - fingerCollection.GetFingerByIndex(Confident_Fingers[1]).X));
                    float temp_Y_pinch = ((1 - fingerCollection.GetFingerByIndex(Confident_Fingers[0]).Y) - (1 - fingerCollection.GetFingerByIndex(Confident_Fingers[1]).Y));
                    float temp_pinch_Dist = (float)Math.Sqrt(Math.Pow(temp_X_pinch, 2) + Math.Pow(temp_Y_pinch, 2));

                    if (pinchSave != -1)
                    {
                        Console.WriteLine("Distance: " + temp_pinch_Dist + ", Difference:" + (temp_pinch_Dist - pinchSave));
                    }

                    pinchSave = temp_pinch_Dist;
                    x_Save = temp_X_pinch;
                    y_Save = temp_Y_pinch;
                }
            }
            catch
            {
                Console.WriteLine("Failed to gather finger collection");
            }

            return 0;
        }
    }
}
