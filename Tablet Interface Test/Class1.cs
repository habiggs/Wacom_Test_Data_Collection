using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using org.matheval;
using System.Windows.Forms;
using WintabDN;
using WacomMTDN;


namespace Tablet_Interface_Test
{
    //----------------------------------------------------------------------
    //Main: Define new tablet, then keep console open
    //----------------------------------------------------------------------
    static class Program
    {
        public static Tablet My_Tablet;

        public static void Main(string[] args)
        {
            //Initialize Data Collector 
            My_Tablet = new Tablet();

            //Keep the program running
            while (true)
            {
                
            }
        }
    }

    //-------------------------------------------------------------------------------------------------------------------------------
    //Tablet Class
    //-------------------------------------------------------------------------------------------------------------------------------
    public class Tablet
    {
        //Data handling objects--------
        public CWintabContext m_logContext = null;
        public  CWintabData m_wtData = null;

        private WTPKT[] Button_State = { 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0};

        //Control Masks----------------
        uint mExpKeysMask;
        uint mTouchRingMask;
        uint mTouchStripMask;

        //public uint pktID;

        //Constructor. Define context, tablet, then set up buttons, then set handler 
        public Tablet()
        {


            //Define Context
            m_logContext = CWintabInfo.GetDefaultSystemContext(ECTXOptionValues.CXO_MESSAGES);

            //Set Context 
            //m_logContext.Options |= (uint)ECTXOptionValues.CXO_MESSAGES; //Enable System Cursor
            m_logContext.Options &= ~(uint)ECTXOptionValues.CXO_SYSTEM; //Disable System Cursor
            m_logContext.Name = "WintabDN Query Data Context";
            m_logContext.SysOrgX = m_logContext.SysOrgY = 0;

            //Add buttons to packets
           mExpKeysMask = CWintabExtensions.GetWTExtensionMask(EWTXExtensionTag.WTX_EXPKEYS2);
            if (mExpKeysMask > 0)
            {
                m_logContext.PktData |= (WTPKT)mExpKeysMask;
            }
            else
            {
                Debug.WriteLine("InitWintab: WTX_EXPKEYS2 mask not found!");
                throw new Exception("Oops - FAILED GetWTExtensionMask for WTX_EXPKEYS2");
            }

            mTouchRingMask = CWintabExtensions.GetWTExtensionMask(EWTXExtensionTag.WTX_TOUCHRING);
            if (mTouchRingMask > 0)
            {
                m_logContext.PktData |= (WTPKT)mTouchRingMask;
            }

            mTouchStripMask = CWintabExtensions.GetWTExtensionMask(EWTXExtensionTag.WTX_TOUCHSTRIP);
            if (mTouchStripMask > 0)
            {
                m_logContext.PktData |= (WTPKT)mTouchStripMask;
            }

            //Open Context
            bool Status = m_logContext.Open();

            //Get tablets
            List<byte> mTabletList = CWintabInfo.GetFoundDevicesIndexList();
            if (mTabletList.Count == 0)
            {
                MessageBox.Show("There are no attached tablets.");
            }

            //Console.WriteLine("Tablets: " + mTabletList.Count());//-------------------------------------------------

            //For each tablet
            foreach (var tabletIdx in mTabletList)
            {
                SetUpTabletExtensions(tabletIdx);
            }

            //Define data handler
            m_wtData = new CWintabData(m_logContext);
            m_wtData.SetWTPacketEventHandler(Handler);
        }

        //For each extension, set up controls---------------------------------------
        private void SetUpTabletExtensions(uint tablet_Idx)
        {
            //Console.WriteLine("Tablet " + tablet_Idx);

            if (mExpKeysMask > 0)
            {
                SetupControlsForExtension(tablet_Idx, EWTXExtensionTag.WTX_EXPKEYS2);
            }

            if (mTouchRingMask > 0)
            {
                SetupControlsForExtension(tablet_Idx, EWTXExtensionTag.WTX_TOUCHRING);
            }

            if (mTouchStripMask > 0)
            {
                SetupControlsForExtension(tablet_Idx, EWTXExtensionTag.WTX_TOUCHSTRIP);
            }
        }

        //Get number of controls in extension, then get number of functions attached to control and set them up--------------------------------------------------------
        private void SetupControlsForExtension(UInt32 tabletIndex_I, EWTXExtensionTag extTagIndex_I)
        {
            uint numCtrls = 0;

            // Get number of controls in this extension;
            if (!CWintabExtensions.ControlPropertyGet(
                m_logContext.HCtx,
                (byte)extTagIndex_I,
                (byte)tabletIndex_I,
                0, // ignored
                0, // ignored
                (ushort)EWTExtensionTabletProperty.TABLET_PROPERTY_CONTROLCOUNT,
                ref numCtrls))
            { throw new Exception("Oops - Failed ControlPropertyGet for TABLET_PROPERTY_CONTROLCOUNT"); }

            //Console.WriteLine("Controls: " + numCtrls);//--------------------------------------------------------

            //For each control in extension
            for (uint ctrlIndex = 0; ctrlIndex < numCtrls; ctrlIndex++)
            {
                uint numFuncs = 0 ;

                //Find number of functions
                if (!CWintabExtensions.ControlPropertyGet(
                m_logContext.HCtx,
                (byte)extTagIndex_I,
                (byte)tabletIndex_I,
                (byte)ctrlIndex,
                0, // ignored
                (ushort)EWTExtensionTabletProperty.TABLET_PROPERTY_FUNCCOUNT,
                ref numFuncs))
                { throw new Exception("Oops - Failed ControlPropertyGet for TABLET_PROPERTY_FUNCCOUNT"); }

                //Console.WriteLine("Functions: " + numFuncs);
                Console.WriteLine("Tablet:" +tabletIndex_I +" , Extension:" + extTagIndex_I + ", Ctrl:" + ctrlIndex + ", #Funcs: "+ numFuncs);
                //for each function attached to control in extension
                for (uint funcIndex = 0; funcIndex < numFuncs; funcIndex++)
                {
                    //---------------------------------------------------------------------------------------------
                    bool bIsAvailable = false;

                    try
                    {

                        WTPKT propOverride = 1;  // true
                        UInt32 ctrlAvailable = 0;
                        UInt32 ctrlLocation = 0;
                        UInt32 ctrlMinRange = 0;
                        UInt32 ctrlMaxRange = 0;
                        String indexStr = extTagIndex_I == EWTXExtensionTag.WTX_EXPKEYS2 ?
                            Convert.ToString(ctrlIndex) :
                            Convert.ToString(funcIndex);

                        // NOTE - you can use strings in any language here.
                        // The strings will be encoded to UTF8 before sent to the driver.
                        // For example, you could use the string: "付録A" to indicate "EK" in Japanese.
                        String ctrlname =
                            extTagIndex_I == EWTXExtensionTag.WTX_EXPKEYS2 ? "EK: " + indexStr :
                            extTagIndex_I == EWTXExtensionTag.WTX_TOUCHRING ? "TR: " + indexStr :
                            extTagIndex_I == EWTXExtensionTag.WTX_TOUCHSTRIP ? "TS: " + indexStr :
                            /* unknown control */                              "UK: " + indexStr;
                        
                        do
                        {
                            // Ask if control is available for override.
                            if (!CWintabExtensions.ControlPropertyGet(
                                m_logContext.HCtx,
                                (byte)extTagIndex_I,
                                (byte)tabletIndex_I,
                                (byte)ctrlIndex,
                                (byte)funcIndex,
                                (ushort)EWTExtensionTabletProperty.TABLET_PROPERTY_AVAILABLE,
                                ref ctrlAvailable))
                            {
                                Debug.WriteLine($"Oops - FAILED ControlPropertyGet for TABLET_PROPERTY_AVAILABLE for tabletIdx: {tabletIndex_I}");
                            }

                            bIsAvailable = (ctrlAvailable > 0);

                            if (!bIsAvailable)
                            {
                                Debug.WriteLine("Cannot override control");
                                break;
                            }

                            // Set flag indicating we're overriding the control.
                            if (!CWintabExtensions.ControlPropertySet(
                               m_logContext.HCtx,
                               (byte)extTagIndex_I,
                               (byte)tabletIndex_I,
                               (byte)ctrlIndex,
                               (byte)funcIndex,
                               (ushort)EWTExtensionTabletProperty.TABLET_PROPERTY_OVERRIDE,
                               propOverride))
                            {
                                Debug.WriteLine($"Oops - FAILED ControlPropertySet for TABLET_PROPERTY_OVERRIDE for tabletIdx: {tabletIndex_I}");
                            }

                            // Set the control name.
                            //ctrlname = "Shitty Name";
                            if (!CWintabExtensions.ControlPropertySet(
                                m_logContext.HCtx,
                                (byte)extTagIndex_I,
                                (byte)tabletIndex_I,
                                (byte)ctrlIndex,
                                (byte)funcIndex,
                                (ushort)EWTExtensionTabletProperty.TABLET_PROPERTY_OVERRIDE_NAME,
                                ctrlname))
                            {
                                Debug.WriteLine($"Oops - FAILED ControlPropertySet for TABLET_PROPERTY_OVERRIDE_NAME for tabletIdx: {tabletIndex_I}");
                            }
                            
                            // Get the location of the control
                            if (!CWintabExtensions.ControlPropertyGet(
                                m_logContext.HCtx,
                                (byte)extTagIndex_I,
                                (byte)tabletIndex_I,
                                (byte)ctrlIndex,
                                (byte)funcIndex,
                                (ushort)EWTExtensionTabletProperty.TABLET_PROPERTY_LOCATION,
                                ref ctrlLocation))
                            {
                                Debug.WriteLine($"Oops - FAILED ControlPropertyGet for TABLET_PROPERTY_LOCATION for tabletIdx: {tabletIndex_I}");
                            }
                            

                            if (!CWintabExtensions.ControlPropertyGet(
                                m_logContext.HCtx,
                                (byte)extTagIndex_I,
                                (byte)tabletIndex_I,
                                (byte)ctrlIndex,
                                (byte)funcIndex,
                                (ushort)EWTExtensionTabletProperty.TABLET_PROPERTY_MIN,
                                ref ctrlMinRange))
                            {
                                Debug.WriteLine($"Oops - FAILED ControlPropertyGet for TABLET_PROPERTY_MIN for tabletIdx: {tabletIndex_I}");
                            }

                            if (!CWintabExtensions.ControlPropertyGet(
                                m_logContext.HCtx,
                                (byte)extTagIndex_I,
                                (byte)tabletIndex_I,
                                (byte)ctrlIndex,
                                (byte)funcIndex,
                                (ushort)EWTExtensionTabletProperty.TABLET_PROPERTY_MAX,
                                ref ctrlMaxRange))
                            {
                                Debug.WriteLine($"Oops - FAILED ControlPropertyGet for TABLET_PROPERTY_MAX for tabletIdx: {tabletIndex_I}");
                            }

                        } 
                        
                        while (false);
                    }

                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString());
                    }
                    //---------------------------------------------------------------------------------------------
                }

            }

        }

        //On Packet Recieved
        public void Handler(object sender, MessageReceivedEventArgs e)
        {  
            if (m_wtData == null)
            {
                return;
            }

            //Handle Pen Data
            if (e.Message.Msg == (int)EWintabEventMessage.WT_PACKET)
            {
                //Get packet ID
                uint pktID = (uint)e.Message.WParam;

                //Initialize pen data packet
                WintabPacket pkt = m_wtData.GetDataPacket((uint)e.Message.LParam, pktID);

                //Get pen data and Output +
                Console.WriteLine(" P:" + pkt.pkNormalPressure + ", X:" + pkt.pkX + ", Y:" + pkt.pkY + ", Z:" + pkt.pkZ + ", Tilt: "+ pkt.pkOrientation.orAltitude + ", Direction: "+ pkt.pkOrientation.orAzimuth + ", Twist:"+ pkt.pkOrientation.orTwist + ", ID#:" + pktID);
            }

            //Handle Extension Data
            if (e.Message.Msg == (int)EWintabEventMessage.WT_PACKETEXT)
            {
                //Get Packet ID
                uint pktID = (uint)e.Message.WParam;

                //Initialize controls packet
                WintabPacketExt pktExt = m_wtData.GetDataPacketExt((uint)e.Message.LParam, pktID);

                if (pktExt.pkBase.nContext == m_logContext.HCtx)
                {
                    Console.WriteLine();
                    Console.WriteLine("Tablet Index: "+ pktExt.pkExpKey.nTablet + " Button Index: " + pktExt.pkExpKey.nControl + ", state: " + pktExt.pkExpKey.nState); //Button Data Output
                    Console.WriteLine("Touch Ring Index: " + pktExt.pkTouchRing.nControl + ", state: " + pktExt.pkTouchRing.nPosition + ", Mode: " + pktExt.pkTouchRing.nMode + ", PktID#: " + pktID); //Touch Wheel Data Output
                }
            }
        }
    }


}