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
                    ElementDefine.CADC_MODE mode = ElementDefine.CADC_MODE.DISABLE;
                    switch (options["CADC Mode"])
                    {
                        case "Disable":
                            mode = ElementDefine.CADC_MODE.DISABLE;
                            break;
                        case "Trigger":
                            mode = ElementDefine.CADC_MODE.TRIGGER;
                            break;
                        case "Consecutive":
                            mode = ElementDefine.CADC_MODE.MOVING;
                            break;
                    }
                    ret = CADCReader.ReadCADC(this, mode);
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


        public override UInt32 GetSystemInfor(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            msg.sm.dic.Clear();
            UInt32 cellnum = (UInt32)parent.CellNum.phydata + 7;    //0~7 means 7~14
            if (cellnum == 14)
            {
                for (byte i = 0; i < 14; i++)
                    msg.sm.dic.Add((uint)(i), true);
            }
            else
            {
                for (byte i = 0; i < 14; i++)
                {
                    if (i < cellnum - 1)
                        msg.sm.dic.Add((uint)i, true);
                    else if (i == cellnum - 1)
                        msg.sm.dic.Add(13, false);
                    else if (i < 13)
                        msg.sm.dic.Add((uint)i, false);
                    else if (i == 13)
                        msg.sm.dic.Add(cellnum - 1, true);
                }
            }

            return ret;
        }
        public override UInt32 Read(ref TASKMessage msg)
        {
            bool bsim = true;
            UInt16 wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            List<byte> OpReglist;

            AutomationElement aElem = parent.m_busoption.GetATMElementbyGuid(AutomationElement.GUIDATMTestStart);
            if (aElem != null)
            {
                bsim |= (aElem.dbValue > 0.0) ? true : false;
                aElem = parent.m_busoption.GetATMElementbyGuid(AutomationElement.GUIDATMTestSimulation);
                bsim |= (aElem.dbValue > 0.0) ? true : false;
            }
            OpReglist = RegisterListGenerator.Generate(ref msg);
            if (OpReglist == null)
                return ret;
            switch (parent.CellNum.phydata)
            {
                case 0:
                    OpReglist.Remove(0x67);
                    OpReglist.Remove(0x68);
                    OpReglist.Remove(0x69);
                    OpReglist.Remove(0x6A);
                    OpReglist.Remove(0x6B);
                    OpReglist.Remove(0x6C);
                    OpReglist.Remove(0x6D);
                    break;
                case 1:
                    OpReglist.Remove(0x68);
                    OpReglist.Remove(0x69);
                    OpReglist.Remove(0x6A);
                    OpReglist.Remove(0x6B);
                    OpReglist.Remove(0x6C);
                    OpReglist.Remove(0x6D);
                    break;
                case 2:
                    OpReglist.Remove(0x69);
                    OpReglist.Remove(0x6A);
                    OpReglist.Remove(0x6B);
                    OpReglist.Remove(0x6C);
                    OpReglist.Remove(0x6D);
                    break;
                case 3:
                    OpReglist.Remove(0x6A);
                    OpReglist.Remove(0x6B);
                    OpReglist.Remove(0x6C);
                    OpReglist.Remove(0x6D);
                    break;
                case 4:
                    OpReglist.Remove(0x6B);
                    OpReglist.Remove(0x6C);
                    OpReglist.Remove(0x6D);
                    break;
                case 5:
                    OpReglist.Remove(0x6C);
                    OpReglist.Remove(0x6D);
                    break;
                case 6:
                    OpReglist.Remove(0x6D);
                    break;
            }

            foreach (byte badd in OpReglist)
            {
                ret = ReadWord(badd, ref wdata);
                parent.m_OpRegImg[badd].err = ret;
                parent.m_OpRegImg[badd].val = wdata;
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    return ret;
            }
            return ret;
        }
        #endregion
    }
}