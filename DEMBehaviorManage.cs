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
    internal class DEMBehaviorManage
    {
        private byte calATECRC;
        private byte calUSRCRC;
        //父对象保存
        private DEMDeviceManage m_parent;
        public DEMDeviceManage parent
        {
            get { return m_parent; }
            set { m_parent = value; }
        }

        private object m_lock = new object();
        private CCommunicateManager m_Interface = new CCommunicateManager();

        public void Init(object pParent)
        {
            parent = (DEMDeviceManage)pParent;
            CreateInterface();

        }

        #region 端口操作
        public bool CreateInterface()
        {
            bool bdevice = EnumerateInterface();
            if (!bdevice) return false;

            return m_Interface.OpenDevice(ref parent.m_busoption);
        }

        public bool DestroyInterface()
        {
            return m_Interface.CloseDevice();
        }

        public bool EnumerateInterface()
        {
            return m_Interface.FindDevices(ref parent.m_busoption);
        }
        #endregion

        #region 操作寄存器操作
        #region 操作寄存器父级操作
        protected UInt32 ReadWord(byte reg, ref UInt16 pval)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnReadWord(reg, ref pval);
            }
            return ret;
        }

        protected UInt32 WriteWord(byte reg, UInt16 val)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnWriteWord(reg, val);
            }
            return ret;
        }

        #endregion

        #region 操作寄存器子级操作
        protected byte crc8_calc(ref byte[] pdata, UInt16 n)
        {
            byte crc = 0;
            byte crcdata;
            UInt16 i, j;

            for (i = 0; i < n; i++)
            {
                crcdata = pdata[i];
                for (j = 0x80; j != 0; j >>= 1)
                {
                    if ((crc & 0x80) != 0)
                    {
                        crc <<= 1;
                        crc ^= 0x07;
                    }
                    else
                        crc <<= 1;

                    if ((crcdata & j) != 0)
                        crc ^= 0x07;
                }
            }
            return crc;
        }

        protected byte calc_crc_read(byte slave_addr, byte reg_addr, UInt16 data)
        {
            byte[] pdata = new byte[5];

            pdata[0] = slave_addr;
            pdata[1] = reg_addr;
            pdata[2] = (byte)(slave_addr | 0x01);
            pdata[3] = SharedFormula.HiByte(data);
            pdata[4] = SharedFormula.LoByte(data);

            return crc8_calc(ref pdata, 5);
        }

        protected byte calc_crc_write(byte slave_addr, byte reg_addr, UInt16 data)
        {
            byte[] pdata = new byte[4];

            pdata[0] = slave_addr; ;
            pdata[1] = reg_addr;
            pdata[2] = SharedFormula.HiByte(data);
            pdata[3] = SharedFormula.LoByte(data);

            return crc8_calc(ref pdata, 4);
        }

        protected UInt32 OnReadWord(byte reg, ref UInt16 pval)
        {
#if debug
            pval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
#if functiontimeout
            ret = LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
#else
            
#if pec
            ret = LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
#endif

#endif
            return ret;
#else
            byte bCrc = 0;
            UInt16 wdata = 0;
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[3];
            byte[] receivebuf = new byte[3];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            try
            {
                sendbuf[0] = (byte)parent.m_busoption.GetOptionsByGuid(BusOptions.I2CAddress_GUID).SelectLocation.Code;
            }
            catch (System.Exception ex)
            {
                return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
            }
            sendbuf[1] = reg;
            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (m_Interface.ReadDevice(sendbuf, ref receivebuf, ref DataOutLen, 3))
                {
                    bCrc = receivebuf[2];
                    wdata = SharedFormula.MAKEWORD(receivebuf[1], receivebuf[0]);
                    if (bCrc != calc_crc_read(sendbuf[0], sendbuf[1], wdata))
                    {
                        pval = ElementDefine.PARAM_HEX_ERROR;
                        ret = LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
                        Thread.Sleep(10);
                        continue;
                    }
                    else
                    {
                        pval = wdata;
                        ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    }
                    break;
                }
                ret = LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
                Thread.Sleep(10);
            }
            //m_Interface.GetLastErrorCode(ref ret);
            return ret;
#endif
        }

        protected UInt32 OnWriteWord(byte reg, UInt16 val)
        {
#if debug
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
#if functiontimeout
            ret = LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
#else
            
#if pec
            ret = LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
#endif

#endif
            return ret;
#else
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[5];
            byte[] receivebuf = new byte[2];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            try
            {
                sendbuf[0] = (byte)parent.m_busoption.GetOptionsByGuid(BusOptions.I2CAddress_GUID).SelectLocation.Code;
            }
            catch (System.Exception ex)
            {
                return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
            }
            sendbuf[1] = reg;
            sendbuf[2] = SharedFormula.HiByte(val);
            sendbuf[3] = SharedFormula.LoByte(val);
            sendbuf[4] = calc_crc_write(sendbuf[0], sendbuf[1], val);
            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen, 3))
                {
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                ret = LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
                Thread.Sleep(10);
            }
            //m_Interface.GetLastErrorCode(ref ret);
            return ret;
#endif
        }

        #endregion
        #endregion

        #region 基础服务功能设计

        public UInt32 Read(ref TASKMessage msg)
        {
            Reg reg = null;
            bool bsim = true;
            byte baddress = 0;
            UInt16 wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            List<byte> EFUSEReglist = new List<byte>();
            List<byte> OpReglist = new List<byte>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            AutomationElement aElem = parent.m_busoption.GetATMElementbyGuid(AutomationElement.GUIDATMTestStart);
            if (aElem != null)
            {
                bsim |= (aElem.dbValue > 0.0) ? true : false;
                aElem = parent.m_busoption.GetATMElementbyGuid(AutomationElement.GUIDATMTestSimulation);
                bsim |= (aElem.dbValue > 0.0) ? true : false;
            }

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                if ((p.guid & ElementDefine.SectionMask) == ElementDefine.VirtualElement)    //略过虚拟参数
                    continue;
                if (p == null) break;
                foreach (KeyValuePair<string, Reg> dic in p.reglist)
                {
                    reg = dic.Value;
                    baddress = (byte)reg.address;
                    OpReglist.Add(baddress);
                }
            }
            OpReglist = OpReglist.Distinct().ToList();

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

        public UInt32 Write(ref TASKMessage msg)
        {
            Reg reg = null;
            byte baddress = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            List<byte> OpReglist = new List<byte>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                if ((p.guid & ElementDefine.SectionMask) == ElementDefine.VirtualElement)    //略过虚拟参数
                    continue;
                if (p == null) break;
                foreach (KeyValuePair<string, Reg> dic in p.reglist)
                {
                    reg = dic.Value;
                    baddress = (byte)reg.address;
                    OpReglist.Add(baddress);
                }
            }

            OpReglist = OpReglist.Distinct().ToList();

            foreach (byte badd in OpReglist)
            {
                ret = WriteWord(badd, parent.m_OpRegImg[badd].val);
                parent.m_OpRegImg[badd].err = ret;
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    return ret;
            }

            return ret;
        }

        public UInt32 BitOperation(ref TASKMessage msg)
        {
            Reg reg = null;
            byte baddress = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<byte> OpReglist = new List<byte>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                if ((p.guid & ElementDefine.SectionMask) == ElementDefine.VirtualElement)    //略过虚拟参数
                    continue;
                switch (p.guid & ElementDefine.SectionMask)
                {
                    case ElementDefine.OperationElement:
                        {
                            if (p == null) break;
                            foreach (KeyValuePair<string, Reg> dic in p.reglist)
                            {
                                reg = dic.Value;
                                baddress = (byte)reg.address;

                                parent.m_OpRegImg[baddress].val = 0x00;
                                parent.WriteToRegImg(p, 1);
                                OpReglist.Add(baddress);

                            }
                            break;
                        }
                }
            }

            OpReglist = OpReglist.Distinct().ToList();

            //Write 
            foreach (byte badd in OpReglist)
            {
                ret = WriteWord(badd, parent.m_OpRegImg[badd].val);
                parent.m_OpRegImg[badd].err = ret;
            }

            return ret;
        }

        public UInt32 ConvertHexToPhysical(ref TASKMessage msg)
        {
            Parameter param = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<Parameter> EFUSEParamList = new List<Parameter>();
            List<Parameter> OpParamList = new List<Parameter>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            foreach (Parameter p in demparameterlist.parameterlist)
            {
                if ((p.guid & ElementDefine.SectionMask) == ElementDefine.VirtualElement)    //略过虚拟参数
                    continue;
                if (p == null) break;
                OpParamList.Add(p);
            }
            OpParamList = OpParamList.Distinct().ToList();

            for (int i = 0; i < OpParamList.Count; i++)
            {
                param = (Parameter)OpParamList[i];
                if (param == null) continue;
                m_parent.Hex2Physical(ref param);
            }

            return ret;
        }

        public UInt32 ConvertPhysicalToHex(ref TASKMessage msg)
        {
            Parameter param = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<Parameter> OpParamList = new List<Parameter>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;


            foreach (Parameter p in demparameterlist.parameterlist)
            {
                if ((p.guid & ElementDefine.SectionMask) == ElementDefine.VirtualElement)    //略过虚拟参数
                    continue;
                if (p == null) break;
                OpParamList.Add(p);
            }
            OpParamList = OpParamList.Distinct().ToList();

            for (int i = 0; i < OpParamList.Count; i++)
            {
                param = (Parameter)OpParamList[i];
                if (param == null) continue;
                if ((param.guid & ElementDefine.SectionMask) == ElementDefine.TemperatureElement) continue;

                m_parent.Physical2Hex(ref param);
            }

            return ret;
        }


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

        private UInt32 ReadSAR(ref TASKMessage sarmsg)
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
        private UInt32 ActiveModeCheck()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            /*ushort tmp = 0;
            ret = ReadWord(0x57, ref tmp);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;
            if ((tmp & 0x0080) != 0x0080)
            {
                ret = ElementDefine.IDS_ERR_DEM_ACTIVE_MODE_ERROR;
            }*/
            return ret;
        }
        public UInt32 Command(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            switch ((ElementDefine.COMMAND)msg.sub_task)
            {
                #region trim
                case ElementDefine.COMMAND.SLOP_TRIM:
                    Parameter[] voltageparams = new Parameter[14];
                    Parameter tempparam = new Parameter();
                    Parameter sarcurrparam = new Parameter();
                    Parameter ccurrparam = new Parameter();
                    Parameter vbatt = new Parameter();
                    ParamContainer demparameterlist = msg.task_parameterlist;
                    if (demparameterlist == null) return ret;

                    for (ushort i = 0; i < demparameterlist.parameterlist.Count; i++)
                    {
                        Parameter param = demparameterlist.parameterlist[i];
                        param.sphydata = String.Empty;
                        if (param.guid <= 0x00036e00 && param.guid >= 0x00036100)
                        {
                            byte index = (byte)((param.guid - 0x00036100) >> 8);
                            voltageparams[index] = param;
                        }
                        else if (param.guid == 0x00037600)
                        {
                            tempparam = param;
                        }
                        else if (param.guid == 0x00037500)
                        {
                            sarcurrparam = param;
                        }
                        else if (param.guid == 0x00033900)
                        {
                            ccurrparam = param;
                        }
                        else if (param.guid == 0x00037c00)
                        {
                            vbatt = param;
                        }

                    }

                    ret = WriteWord(0x0F, 0x3714);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                    ret = WriteWord(0x0b, 0x00);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    #region Enter Efuse Mode
                    ret = WriteWord(0x18, 0x8000);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    ret = WriteWord(0x18, 0x8001);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    #endregion
                    #region Clear Offset
                    ret = WriteWord(0x93, 0);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    ret = WriteWord(0x9a, 0);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    ret = WriteWord(0x9b, 0);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    ret = WriteWord(0x9c, 0);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    ret = WriteWord(0x9d, 0);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    ret = WriteWord(0x9e, 0);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    ret = WriteWord(0x9f, 0);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                    #endregion

                    #region voltage
                    for (ushort code = 0; code < 16; code++)
                    {
                        #region write code
                        ret = WriteWord(0x94, (ushort)((code << 12) | (code << 8) | (code << 4) | code));
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        ret = WriteWord(0x95, (ushort)((code << 12) | (code << 8) | (code << 4) | code));
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        ret = WriteWord(0x96, (ushort)((code << 12) | (code << 8) | (code << 4) | code));
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        ret = WriteWord(0x97, (ushort)((code << 12) | (code << 8)));
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        #endregion
                        Thread.Sleep(100);
                        #region read adc
                        ushort[] voltageadc = new ushort[14];
                        for (byte i = 0; i < 14; i++)
                        {
                            ret = ReadWord((byte)(i + 0x61), ref voltageadc[i]);
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        }
                        #endregion
                        #region calculate
                        double[] voltagephy = new double[14];
                        for (byte i = 0; i < 14; i++)
                        {
                            short s = (short)voltageadc[i];
                            voltagephy[i] = s * 0.625 / 4;
                        }
                        #endregion
                        #region save
                        for (byte i = 0; i < 14; i++)
                        {
                            voltageparams[i].sphydata += voltagephy[i].ToString() + ",";
                        }
                        #endregion
                    }
                    #endregion
                    #region temp
                    for (ushort code = 0; code < 16; code++)
                    {
                        #region write code
                        ret = WriteWord(0x99, (ushort)(code << 12));
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        #endregion
                        Thread.Sleep(100);
                        #region read adc
                        ushort tempadc = new ushort();
                        ret = ReadWord(0x76, ref tempadc);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        #endregion
                        #region calculate
                        double tempphy = new double();
                        short s = (short)tempadc;
                        tempphy = s * 0.3125 / 4;
                        #endregion
                        #region save
                        tempparam.sphydata += tempphy.ToString() + ",";
                        #endregion
                    }
                    #endregion
                    #region sar current
                    for (ushort code = 0; code < 32; code++)
                    {
                        #region write code
                        ret = WriteWord(0x99, (ushort)(code));
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        #endregion
                        Thread.Sleep(100);
                        #region read adc
                        ushort sarcurradc = 0;
                        ret = ReadWord((byte)(0x75), ref sarcurradc);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        #endregion
                        #region calculate
                        double sarcurrphy = 0;
                        short s = (short)sarcurradc;
                        sarcurrphy = s * 31.25 / 4;
                        #endregion
                        #region save
                        sarcurrparam.sphydata += sarcurrphy.ToString() + ",";
                        #endregion
                    }
                    #endregion
                    #region cadc
                    for (ushort code = 0; code < 256; code++)
                    {
                        #region write code
                        ret = WriteWord(0x93, (ushort)(code));
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        #endregion
                        Thread.Sleep(100);
                        #region read adc
                        ret = WriteWord(0x01, 0x0002);  //clear cadc trigger flag
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        ret = WriteWord(0x38, 0x86);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        Thread.Sleep(265);
                        ////////////////////////////////////////////////////////
                        ushort flag = 0;
                        bool ready = false;
                        for (int i = 0; i < 50; i++)
                        {
                            ret = ReadWord(0x01, ref flag);
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                            if ((flag & 0x0002) == 0x0002)
                            {
                                ready = true;
                                break;
                            }
                            Thread.Sleep(20);
                        }
                        if (!ready)
                            return LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
                        /////////////////////////////////////////////////////////
                        ushort ccurradc = 0;
                        ret = ReadWord((byte)(0x39), ref ccurradc);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        #endregion
                        #region calculate
                        double ccurrphy = 0;
                        short s = (short)ccurradc;
                        ccurrphy = s * 7.8125;
                        #endregion
                        #region save
                        ccurrparam.sphydata += ccurrphy.ToString() + ",";
                        #endregion
                    }
                    #endregion
                    #region vbatt
                    for (ushort code = 0; code < 16; code++)
                    {
                        #region write code
                        ret = WriteWord(0x99, (ushort)(code << 8));
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        #endregion
                        Thread.Sleep(100);
                        #region read adc
                        ushort vbattadc = 0;
                        ret = ReadWord((byte)(0x7c), ref vbattadc);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        #endregion
                        #region calculate
                        double vbattphy = 0;
                        vbattphy = vbattadc * 12.5 / 4;
                        #endregion
                        #region save
                        vbatt.sphydata += vbattphy.ToString() + ",";
                        #endregion
                    }
                    #endregion
                    break;
                #endregion
                #region Scan SFL commands
                case ElementDefine.COMMAND.OPTIONS:

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
                            ret = ReadSAR(ref sarmsg);
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
                case ElementDefine.COMMAND.SCS:
                    if (msg.task_parameterlist.parameterlist[0].guid == ElementDefine.TRIGGER_CADC)
                    {
                        ret = ReadCADC(ElementDefine.CADC_MODE.TRIGGER);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                    }
                    else
                    {
                        //ret = ReadSAR(ref msg);
                        Read(ref msg);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                    }

                    ret = ConvertHexToPhysical(ref msg);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    break;
                case ElementDefine.COMMAND.PASSWORD:
                    msg.percent = 20;
                    ret = GetRegisteInfor(ref msg);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    msg.percent = 30;
                    ret = Read(ref msg);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    msg.percent = 40;
                    ret = parent.ConvertPhysicalToHex(ref msg);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;

                    msg.percent = 50;
                    ret = WriteWord(0x0f, 0x3714);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;

                    msg.percent = 60;
                    ret = Write(ref msg);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    msg.percent = 70;
                    ret = Read(ref msg);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    msg.percent = 80;
                    ret = parent.ConvertHexToPhysical(ref msg);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;

                    msg.percent = 90;
                    ret = WriteWord(0x0f, 0x0);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    break;
                case ElementDefine.COMMAND.REGISTER_CONFIG_SAVE_HEX:
                    {
                        InitRegisterData();
                        ret = ConvertPhysicalToHex(ref msg);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        string HexData = GetRegisterHexData(ref msg);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        FileStream file = new FileStream(msg.sub_task_json, FileMode.Create);
                        StreamWriter sw = new StreamWriter(file);
                        sw.Write(HexData);
                        sw.Close();
                        file.Close();
                        break;
                    }
            }
            return ret;
        }
        private void InitRegisterData()
        {
            for (ushort i = ElementDefine.OP_USR_OFFSET; i <= ElementDefine.OP_USR_TOP; i++)
            {
                parent.m_OpRegImg[i].err = 0;
                parent.m_OpRegImg[i].val = 0;
            }
        }

        private string GetRegisterHexData(ref TASKMessage msg)
        {
            string tmp = "";
            for (ushort i = ElementDefine.OP_USR_OFFSET; i <= ElementDefine.OP_USR_TOP; i++)
            {
                if (parent.m_OpRegImg[i].err != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    return "";
                tmp += "0x" + i.ToString("X2") + ", " + "0x" + parent.m_OpRegImg[i].val.ToString("X4") + "\r\n";
            }
            return tmp;
        }

        public UInt32 EpBlockRead()
        {
            ushort wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            return ret;
        }
        #endregion

        #region 特殊服务功能设计
        public UInt32 GetDeviceInfor(ref DeviceInfor deviceinfor)
        {
#if debug
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
#else
            string shwversion = String.Empty;
            UInt16 wval = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = ReadWord(0x00, ref wval);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            deviceinfor.status = 0;
            deviceinfor.type = wval;

            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                LibErrorCode.UpdateDynamicalErrorDescription(ret, new string[] { deviceinfor.shwversion });

            return ret;
#endif
        }

        public UInt32 GetSystemInfor(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            return ret;
        }

        public UInt32 GetRegisteInfor(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            return ret;
        }
        #endregion
    }
}