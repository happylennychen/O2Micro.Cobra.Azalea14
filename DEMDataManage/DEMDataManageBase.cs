using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using O2Micro.Cobra.Common;

namespace O2Micro.Cobra.Azalea14
{
    internal class DEMDataManageBase
    {
        //父对象保存
        private DEMBehaviorManageBase m_parent;
        public DEMBehaviorManageBase parent
        {
            get { return m_parent; }
            set { m_parent = value; }
        }

        /// <summary>
        /// 硬件模式下相关参数数据初始化
        /// </summary>
        public DEMDataManageBase(object pParent)
        {
            parent = (DEMBehaviorManageBase)pParent;
        }

        /// <summary>
        /// 更新参数ItemList
        /// </summary>
        /// <param name="pTarget"></param>
        /// <param name="relatedparameters"></param>
        /// <returns></returns>
        public virtual void UpdateEpParamItemList(Parameter pTarget)
        {
        }

        /// <summary>
        /// 转换参数值类型从物理值到16进制值
        /// </summary>
        /// <param name="p"></param>
        /// <param name="relatedparameters"></param>
        public virtual void Physical2Hex(ref Parameter p)
        {
            UInt16 wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            if (p == null) return;
            switch ((ElementDefine.SUBTYPE)p.subtype)
            {
                case ElementDefine.SUBTYPE.EXT_TEMP:
                    {
                        ushort Cref = 0;
                        switch (parent.parent.m_OpRegImg[0x11].val & 0x03)
                        {
                            case 0x01: Cref = 20; break;
                            case 0x02: Cref = 120; break;
                            default: break;
                        }

                        double r = TempToResist(p.phydata);
                        double v = r / 1000.0 * Cref;


                        double tmp = v - p.offset;
                        tmp = tmp * p.regref;
                        tmp = tmp / p.phyref;
                        wdata = (UInt16)(tmp);

                        ret = WriteToRegImg(p, wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
                default:
                    {
                        double tmp = p.phydata - p.offset;
                        tmp = tmp * p.regref;
                        tmp = tmp / p.phyref;
                        wdata = (UInt16)(tmp);
                        ret = WriteToRegImg(p, wdata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
            }
        }

        /// <summary>
        /// 转换参数值类型从物理值到16进制值
        /// </summary>
        /// <param name="p"></param>
        /// <param name="relatedparameters"></param>
        public virtual void Hex2Physical(ref Parameter p)
        {
            UInt16 wdata = 0;
            double ddata = 0;
            short sdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            if (p == null) return;
            switch ((ElementDefine.SUBTYPE)p.subtype)
            {
                case ElementDefine.SUBTYPE.VOLTAGE:
                    ret = ReadSignedFromRegImg(p, ref sdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    {
                        p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                        break;
                    }
                    p.phydata = Regular2Physical(sdata, p.regref, p.phyref);
                    break;
                case ElementDefine.SUBTYPE.INT_TEMP:
                    ret = ReadSignedFromRegImg(p, ref sdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    {
                        p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                        break;
                    }
                    ddata = Regular2Physical(sdata, p.regref, p.phyref);
                    //p.phydata = (double)((ddata - 1252.5) / 4.345 + 23.0);
                    //p.phydata = (double)((ddata - 1220.0) / 4.25 + 23.0);  //Kevin
                    p.phydata = (double)((ddata - 1280.0) / 4.25 + 23.0);   //Kevin v2

                    break;
                case ElementDefine.SUBTYPE.EXT_TEMP:        //Scan SFL把这里污染了
                    int index = 0;
                    switch (p.guid)
                    {
                        case ElementDefine.THM0: index = 0; break;
                        case ElementDefine.THM1: index = 1; break;
                    }

                    ret = ReadFromRegImg(p, ref wdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    {
                        p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                        break;
                    }
                    ushort Cref = 0;
                    Cref = parent.parent.thms[index].thm_crrt;
                    ddata = Regular2Physical(wdata, p.regref, p.phyref);
                    ddata = ddata * 1000 / Cref;
                    p.phydata = ResistToTemp(ddata);
                    break;
                case ElementDefine.SUBTYPE.SAR_CURRENT:
                    ret = ReadFromRegImg(p, ref wdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    {
                        p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                        break;
                    }
                    sdata = (short)wdata;
                    p.phydata = sdata * p.phyref * 1000 / parent.parent.rsense; //需要带符号
                    break;
                case ElementDefine.SUBTYPE.CADC:
                    if (parent.parent.cadc_mode == ElementDefine.CADC_MODE.DISABLE)
                        wdata = 0;
                    else if (parent.parent.cadc_mode == ElementDefine.CADC_MODE.TRIGGER)
                    {
                        wdata = parent.parent.m_OpRegImg[0x39].val;
                        ret = parent.parent.m_OpRegImg[0x39].err;
                    }
                    else if (parent.parent.cadc_mode == ElementDefine.CADC_MODE.MOVING)
                    {
                        wdata = parent.parent.m_OpRegImg[0x17].val;
                        ret = parent.parent.m_OpRegImg[0x17].err;
                    }
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    {
                        p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                        break;
                    }
                    sdata = (short)wdata;
                    p.hexdata = wdata;
                    p.phydata = sdata * p.phyref * 1000 / parent.parent.rsense; //需要带符号
                    break;
                case ElementDefine.SUBTYPE.COULOMB_COUNTER:
                    {
                        UInt32 udata = 0;
                        Int32 idata = 0;
                        //ret = ReadSignedFromRegImg(p, ref idata);
                        udata = parent.parent.m_OpRegImg[0x3a].val;
                        udata <<= 16;
                        udata |= parent.parent.m_OpRegImg[0x3b].val;
                        idata = (int)udata;
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        /*
                        ddata = (double)((double)idata * p.phyref / p.regref);   //uVs
                        ddata = ddata / parent.rsense;  //mAs
                        ddata /= 3600;               //mAH  */
                        ddata = ((double)idata * 0.0078125 * 0.128) / (3600 * parent.parent.rsense);
                        p.phydata = ddata;
                        break;
                    }
                default:
                    ret = ReadFromRegImg(p, ref wdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    {
                        p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                        break;
                    }
                    ddata = (double)((double)wdata * p.phyref / p.regref);
                    p.phydata = ddata + p.offset;
                    break;
            }
        }

        #region General functions
        /// <summary>
        /// 转换Hex -> Physical
        /// </summary>
        /// <param name="sVal"></param>
        /// <param name="RegularRef"></param>
        /// <param name="PhysicalRef"></param>
        /// <returns></returns>
        protected double Regular2Physical(UInt16 wVal, double RegularRef, double PhysicalRef)
        {
            double dval;

            dval = (double)((double)(wVal * PhysicalRef) / (double)RegularRef);

            return dval;
        }

        /// <summary>
        /// 转换Hex -> Physical
        /// </summary>
        /// <param name="sVal"></param>
        /// <param name="RegularRef"></param>
        /// <param name="PhysicalRef"></param>
        /// <returns></returns>
        protected double Regular2Physical(short sVal, double RegularRef, double PhysicalRef)
        {
            double dval;

            dval = (double)((double)(sVal * PhysicalRef) / (double)RegularRef);

            return (double)dval;
        }
        /// <summary>
        /// 转换Physical -> Hex
        /// </summary>
        /// <param name="fVal"></param>
        /// <param name="RegularRef"></param>
        /// <param name="PhysicalRef"></param>
        /// <returns></returns>
        protected UInt16 Physical2Regular(float fVal, double RegularRef, double PhysicalRef)
        {
            UInt16 wval;
            double dval, integer, fraction;

            dval = (double)((double)(fVal * RegularRef) / (double)PhysicalRef);
            integer = Math.Truncate(dval);
            fraction = (double)(dval - integer);
            if (fraction >= 0.5)
                integer += 1;
            if (fraction <= -0.5)
                integer -= 1;
            wval = (UInt16)integer;

            return wval;
        }

        /// <summary>
        /// 从数据buffer中读数据
        /// </summary>
        /// <param name="pval"></param>
        /// <returns></returns>
        protected UInt32 ReadFromRegImg(Parameter p, ref UInt16 pval)
        {
            UInt32 data;
            UInt16 hi = 0, lo = 0;
            Reg regLow = null, regHi = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            foreach (KeyValuePair<string, Reg> dic in p.reglist)
            {
                if (dic.Key.Equals("Low"))
                {
                    regLow = dic.Value;
                    ret = ReadRegFromImg(regLow.address, p.guid, ref lo);
                    lo <<= (16 - regLow.bitsnumber - regLow.startbit); //align with left
                }
                else if (dic.Key.Equals("High"))
                {
                    regHi = dic.Value;
                    ret = ReadRegFromImg(regHi.address, p.guid, ref hi);
                    hi <<= (16 - regHi.bitsnumber - regHi.startbit); //align with left
                    hi >>= (16 - regHi.bitsnumber); //align with right
                }
            }

            data = ((UInt32)(((UInt16)(lo)) | ((UInt32)((UInt16)(hi))) << 16));
            data >>= (16 - regLow.bitsnumber); //align with right

            pval = (UInt16)data;
            p.hexdata = pval;
            return ret;
        }

        /// <summary>
        /// 从数据buffer中读有符号数
        /// </summary>
        /// <param name="pval"></param>
        /// <returns></returns>
        protected UInt32 ReadSignedFromRegImg(Parameter p, ref short pval)
        {
            UInt16 wdata = 0, tr = 0;
            Int16 sdata;
            Reg regLow = null, regHi = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = ReadFromRegImg(p, ref wdata);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return ret;

            foreach (KeyValuePair<string, Reg> dic in p.reglist)
            {
                if (dic.Key.Equals("Low"))
                    regLow = dic.Value;

                if (dic.Key.Equals("High"))
                    regHi = dic.Value;
            }

            if (regHi != null)
                tr = (UInt16)(16 - regHi.bitsnumber - regLow.bitsnumber);
            else
                tr = (UInt16)(16 - regLow.bitsnumber);

            wdata <<= tr;
            sdata = (Int16)wdata;
            sdata = (Int16)(sdata / (1 << tr));

            pval = sdata;
            return ret;
        }


        /// <summary>
        /// 写数据到buffer中
        /// </summary>
        /// <param name="wVal"></param>
        /// <returns></returns>
        public UInt32 WriteToRegImg(Parameter p, UInt16 wVal)
        {
            UInt16 data = 0, lomask = 0, himask = 0;
            UInt16 plo, phi, ptmp;
            //byte hi = 0, lo = 0, tmp = 0;
            Reg regLow = null, regHi = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            p.hexdata = wVal;
            foreach (KeyValuePair<string, Reg> dic in p.reglist)
            {
                if (dic.Key.Equals("Low"))
                    regLow = dic.Value;

                if (dic.Key.Equals("High"))
                    regHi = dic.Value;
            }

            ret = ReadRegFromImg(regLow.address, p.guid, ref data);
            if (regHi == null)
            {
                lomask = (UInt16)((1 << regLow.bitsnumber) - 1);
                lomask <<= regLow.startbit;
                data &= (UInt16)(~lomask);
                data |= (UInt16)(wVal << regLow.startbit);
                WriteRegToImg(regLow.address, p.guid, data);
            }
            else
            {

                lomask = (UInt16)((1 << regLow.bitsnumber) - 1);
                plo = (UInt16)(wVal & lomask);
                himask = (UInt16)((1 << regHi.bitsnumber) - 1);
                himask <<= regLow.bitsnumber;
                phi = (UInt16)((wVal & himask) >> regLow.bitsnumber);

                //mask = (UInt16)((1 << regLow.bitsnumber) - 1);
                lomask <<= regLow.startbit;
                ptmp = (UInt16)(data & ~lomask);
                ptmp |= (UInt16)(plo << regLow.startbit);
                WriteRegToImg(regLow.address, p.guid, ptmp);

                ret |= ReadRegFromImg(regHi.address, p.guid, ref data);
                himask = (UInt16)((1 << regHi.bitsnumber) - 1);
                himask <<= regHi.startbit;
                ptmp = (UInt16)(data & ~himask);
                ptmp |= (UInt16)(phi << regHi.startbit);
                WriteRegToImg(regHi.address, p.guid, ptmp);

            }

            return ret;
        }


        /// <summary>
        /// 写有符号数据到buffer中
        /// </summary>
        /// <param name="wVal"></param>
        /// <param name="pChip"></param>
        /// <returns></returns>
        protected UInt32 WriteSignedToRegImg(Parameter p, Int16 sVal)
        {
            UInt16 wdata, tr = 0;
            Int16 sdata;
            Reg regLow = null, regHi = null;

            sdata = sVal;
            foreach (KeyValuePair<string, Reg> dic in p.reglist)
            {
                if (dic.Key.Equals("Low"))
                    regLow = dic.Value;

                if (dic.Key.Equals("High"))
                    regHi = dic.Value;
            }
            if (regHi != null)
                tr = (UInt16)(16 - regHi.bitsnumber - regLow.bitsnumber);
            else
                tr = (UInt16)(16 - regLow.bitsnumber);

            sdata *= (Int16)(1 << tr);
            wdata = (UInt16)sdata;
            wdata >>= tr;

            return WriteToRegImg(p, wdata);
        }

        protected void WriteToRegImgError(Parameter p, UInt32 err)
        {
        }

        #region EFuse数据缓存操作
        protected UInt32 ReadRegFromImg(UInt16 reg, UInt32 guid, ref UInt16 pval)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            pval = parent.parent.m_OpRegImg[reg].val;
            ret = parent.parent.m_OpRegImg[reg].err;
            return ret;
        }

        protected void WriteRegToImg(UInt16 reg, UInt32 guid, UInt16 value)
        {
            parent.parent.m_OpRegImg[reg].val = value;
            parent.parent.m_OpRegImg[reg].err = LibErrorCode.IDS_ERR_SUCCESSFUL;
        }
        #endregion

        #region 外部温度转换
        public double ResistToTemp(double resist)
        {
            int index = 0;
            Dictionary<Int32, double> m_TempVals = new Dictionary<int, double>();
            Dictionary<Int32, double> m_ResistVals = new Dictionary<int, double>();
            if (parent.parent.tempParamlist == null) return 0;

            foreach (Parameter p in parent.parent.tempParamlist.parameterlist)
            {
                //利用温度参数属性下subtype区分内部/外部温度
                //0:内部温度参数 1： 外部温度参数
                if ((ElementDefine.SUBTYPE)p.subtype == ElementDefine.SUBTYPE.EXT_TEMP_TABLE)
                {
                    m_TempVals.Add(index, p.key);
                    m_ResistVals.Add(index, p.phydata);
                    index++;
                }
            }
            return SharedFormula.ResistToTemp(resist, m_TempVals, m_ResistVals);
        }

        public double TempToResist(double temp)
        {
            int index = 0;
            Dictionary<Int32, double> m_TempVals = new Dictionary<int, double>();
            Dictionary<Int32, double> m_ResistVals = new Dictionary<int, double>();
            if (parent.parent.tempParamlist == null) return 0;

            foreach (Parameter p in parent.parent.tempParamlist.parameterlist)
            {
                //利用温度参数属性下subtype区分内部/外部温度
                //0:内部温度参数 1： 外部温度参数
                if ((ElementDefine.SUBTYPE)p.subtype == ElementDefine.SUBTYPE.EXT_TEMP_TABLE)
                {
                    m_TempVals.Add(index, p.key);
                    m_ResistVals.Add(index, p.phydata);
                    index++;
                }
            }

            return SharedFormula.TempToResist(temp, m_TempVals, m_ResistVals);
        }
        #endregion
        #endregion
    }
}
