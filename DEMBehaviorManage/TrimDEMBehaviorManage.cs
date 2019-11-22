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
    internal class TrimDEMBehaviorManage : DEMBehaviorManageBase
    {
        #region 基础服务功能设计
        public override UInt32 Command(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            switch ((ElementDefine.COMMAND)msg.sub_task)
            {
                #region trim
                case ElementDefine.COMMAND.TRIM_SLOP:
                    Parameter[] voltageparams = new Parameter[14];
                    Parameter tempparam = new Parameter();
                    Parameter sarcurrparam = new Parameter();
                    Parameter ccurrparam = new Parameter();
                    Parameter vbatt = new Parameter();
                    ParamContainer demparameterlist = msg.task_parameterlist;
                    if (demparameterlist == null) return ret;

                    byte cellnum = GetCellNumber();
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
                    //for (ushort code = 0; code < 16; code++)
                    {
                        #region write code
                        //ret = WriteWord(0x94, (ushort)((code << 12) | (code << 8) | (code << 4) | code));
                        //if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        //ret = WriteWord(0x95, (ushort)((code << 12) | (code << 8) | (code << 4) | code));
                        //if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        //ret = WriteWord(0x96, (ushort)((code << 12) | (code << 8) | (code << 4) | code));
                        //if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        //ret = WriteWord(0x97, (ushort)((code << 12) | (code << 8)));
                        //if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        #endregion
                        Thread.Sleep(100);
                        #region read adc
                        ushort[] voltageadc = new ushort[cellnum];
                        for (byte i = 0; i < cellnum; i++)
                        {
                            byte address = 0;
                            if (i == cellnum - 1)
                                address = 0x6E;
                            else
                                address = (byte)(i + 0x61);
                            ret = ReadWord(address, ref voltageadc[i]);
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
                        }
                        #endregion
                        #region calculate
                        double[] voltagephy = new double[cellnum];
                        for (byte i = 0; i < cellnum; i++)
                        {
                            short s = (short)voltageadc[i];
                            voltagephy[i] = s * 0.625 / 4;
                        }
                        #endregion
                        #region save
                        for (byte i = 0; i < cellnum; i++)
                        {
                            voltageparams[i].sphydata += voltagephy[i].ToString() + ",";
                        }
                        #endregion
                    }
                    #endregion
                    #region temp
                    //for (ushort code = 0; code < 16; code++)
                    {
                        #region write code
                        //ret = WriteWord(0x99, (ushort)(code << 12));
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
                    //for (ushort code = 0; code < 32; code++)
                    {
                        #region write code
                        //ret = WriteWord(0x99, (ushort)(code));
                        //if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
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
                    //for (ushort code = 0; code < 256; code++)
                    {
                        #region write code
                        //ret = WriteWord(0x93, (ushort)(code));
                        //if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
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
                    //for (ushort code = 0; code < 16; code++)
                    {
                        #region write code
                        //ret = WriteWord(0x99, (ushort)(code << 8));
                        //if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
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
            }
            return ret;
        }

        private byte GetCellNumber()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ushort buf = 0;
            ret = ReadWord(0x08, ref buf);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return 0;
            byte cellnum = (byte)(((buf & 0x7000) >> 12) + 7);
            return cellnum;
        }
        #endregion
    }
}