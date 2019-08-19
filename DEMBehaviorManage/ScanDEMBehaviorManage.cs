//#define debug
//#if debug
//#define functiontimeout
//#define pec
//#define frozen
//#define dirty
//#define readback
//#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using O2Micro.Cobra.Communication;
using O2Micro.Cobra.Common;
using System.IO;

namespace O2Micro.Cobra.Azalea14
{
    internal class ScanDEMBehaviorManage:DEMBehaviorManageBase
    {
        #region 基础服务功能设计


        #region SAR

        void GetExtTemp()
        {
            for (byte i = 0; i < 2; i++)
            {
                parent.thms[i].ADC2 = parent.m_OpRegImg[0x76 + i].val;  //120uA时的电压值

                if (parent.thms[i].ADC2 <= 32700)   //120uA档是正确值
                {
                    parent.m_OpRegImg[0x76 + i].val = parent.thms[i].ADC2;
                    parent.thms[i].thm_crrt = 120;
                }
                else    //20uA档是正确值
                {
                    parent.m_OpRegImg[0x76 + i].val = parent.thms[i].ADC1;
                    parent.thms[i].thm_crrt = 20;
                }

                parent.m_OpRegImg[0x76 + i].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
            }
        }

        private UInt32 ScanReadSAR(ref TASKMessage sarmsg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            TASKMessage tmpmsg = new TASKMessage();  //only contains temperature parameters

            ushort thm_crrt_sel = 0;
            ret = ReadWord(0x11, ref thm_crrt_sel); //保存原始值
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;

            ret = WriteWord(0x11, 0x0001);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;
            ret = Read(ref sarmsg);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;

            for (byte i = 0; i < 2; i++)
            {
                parent.thms[i].ADC1 = parent.m_OpRegImg[0x76 + i].val;  //20uA时的电压值
            }


            ret = WriteWord(0x11, 0x0002);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;

            foreach (Parameter p in sarmsg.task_parameterlist.parameterlist)
            {
                if (p.subtype == (ushort)ElementDefine.SUBTYPE.EXT_TEMP)
                    tmpmsg.task_parameterlist.parameterlist.Add(p);
            }
            ret = Read(ref tmpmsg);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;

            GetExtTemp();

            ret = WriteWord(0x11, thm_crrt_sel);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;
            return ret;
        }
        #endregion
        private UInt32 ReadCADC(ElementDefine.CADC_MODE mode)       //MP version new method. Do 4 time average by HW, and we can also have the trigger flag and coulomb counter work at the same time.
        {
            parent.cadc_mode = mode;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ushort temp = 0;
            switch (mode)
            {
                case ElementDefine.CADC_MODE.DISABLE:
                    #region disable
                    ret = WriteWord(0x38, 0x00);        //clear all
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    #endregion
                    break;
                case ElementDefine.CADC_MODE.MOVING:
                    #region moving mode
                    ret = ActiveModeCheck();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    bool cadc_moving_flag = false;
                    {
                        ret = WriteWord(0x01, 0x0004);        //Clear cadc_moving_flag
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        ret = WriteWord(0x38, 0x18);        //Set cc_always_enable, moving_average_enable, sw_cadc_ctrl=0b00
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        for (byte i = 0; i < ElementDefine.CADC_RETRY_COUNT; i++)
                        {
                            Thread.Sleep(30);
                            ret = ReadWord(0x01, ref temp);
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                                return ret;
#if debug
                    cadc_moving_flag = true;
                    break;
#else
                            if ((temp & 0x0004) == 0x0004)
                            {
                                cadc_moving_flag = true;
                                break;
                            }
#endif
                        }
                        if (cadc_moving_flag)   //转换完成
                        {
#if debug
                    temp = 15;
#else
                            ret = ReadWord(0x17, ref temp);
#endif
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                                return ret;
                        }
                        else
                        {
                            ret = ElementDefine.IDS_ERR_DEM_READCADC_TIMEOUT;
                        }
                    }
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;

                    parent.m_OpRegImg[0x17].err = ret;
                    parent.m_OpRegImg[0x17].val = temp;
                    #endregion
                    break;
                case ElementDefine.CADC_MODE.TRIGGER:
                    #region trigger mode
                    ret = ActiveModeCheck();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    bool cadc_trigger_flag = false;
                    {
                        ret = WriteWord(0x01, 0x0002);        //Clear cadc_trigger_flag
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        ret = WriteWord(0x38, 0x06);        //Set cadc_one_or_four, sw_cadc_ctrl=0b10
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        for (byte i = 0; i < ElementDefine.CADC_RETRY_COUNT; i++)
                        {
                            Thread.Sleep(60);
                            ret = ReadWord(0x01, ref temp);
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                                return ret;
#if debug
                            cadc_trigger_flag = true;
                    break;
#else
                            if ((temp & 0x0002) == 0x0002)
                            {
                                cadc_trigger_flag = true;
                                break;
                            }
#endif
                        }
                        if (cadc_trigger_flag)   //转换完成
                        {
#if debug
                    temp = 15;
#else
                            ret = ReadWord(0x39, ref temp);
#endif
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                                return ret;
                        }
                        else
                        {
                            ret = ElementDefine.IDS_ERR_DEM_READCADC_TIMEOUT;
                        }
                    }
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;

                    parent.m_OpRegImg[0x39].err = ret;
                    parent.m_OpRegImg[0x39].val = temp;
                    #endregion
                    break;
            }

            return ret;
        }
        public override UInt32 Command(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            switch ((ElementDefine.COMMAND)msg.sub_task)
            {
                #region Scan SFL commands
                case ElementDefine.COMMAND.SCAN_OPTIONS:

                    TASKMessage sarmsg = new TASKMessage();  //only contains sar adc parameters
                    TASKMessage noadcmsg = new TASKMessage();  //only contains none parameters

                    foreach (Parameter p in msg.task_parameterlist.parameterlist)
                    {
                        byte addr = (byte)((p.guid & 0x0000ff00) >> 8);
                        if (p.guid == ElementDefine.TRIGGER_CADC || p.guid == ElementDefine.MOVING_CADC)
                        { }
                        else if (addr >= 0x60 && addr <= 0x7f)
                            sarmsg.task_parameterlist.parameterlist.Add(p);
                        else
                        {
                            noadcmsg.task_parameterlist.parameterlist.Add(p);
                        }
                    }

                    var options = SharedAPI.DeserializeStringToDictionary<string, string>(msg.sub_task_json);
                    switch (options["SAR ADC Mode"])
                    {
                        case "Disable":
                            //ret = ReadSAR(ref msg, ElementDefine.SAR_MODE.DISABLE);
                            break;
                        case "8_Time_Average":
                            ret = ScanReadSAR(ref sarmsg);
                            break;
                    }
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    switch (options["CADC Mode"])
                    {
                        case "Disable":
                            ret = ReadCADC(ElementDefine.CADC_MODE.DISABLE);
                            break;
                        case "Trigger":
                            ret = ReadCADC(ElementDefine.CADC_MODE.TRIGGER);
                            break;
                        case "Consecutive":
                            ret = ReadCADC(ElementDefine.CADC_MODE.MOVING);
                            break;
                    }
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    Read(ref noadcmsg);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    break;
                #endregion
            }
            return ret;
        }
        #endregion
    }
}