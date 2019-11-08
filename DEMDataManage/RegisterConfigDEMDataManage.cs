using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using O2Micro.Cobra.Common;

namespace O2Micro.Cobra.Azalea14
{
    public class RegisterConfigDEMDataManage:DEMDataManageBase
    {

        public RegisterConfigDEMDataManage(object pParent) : base(pParent)
        {
        }
        /// <summary>
        /// 更新参数ItemList
        /// </summary>
        /// <param name="pTarget"></param>
        /// <param name="relatedparameters"></param>
        /// <returns></returns>
        public override void UpdateEpParamItemList(Parameter pTarget)
        {
            if (pTarget.errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return;
            switch (pTarget.guid)
            {
                case ElementDefine.THM0_OT_TH:
                case ElementDefine.THM0_OTR_TH:
                case ElementDefine.THM1_OT_TH:
                case ElementDefine.THM1_OTR_TH:
                    if (parent.parent.pTHM_CRRT_SEL.phydata == 0 || parent.parent.pTHM_CRRT_SEL.phydata == 3) //disabled
                    {
                        pTarget.dbPhyMin = ResistToTemp(999999);
                        pTarget.dbPhyMax = ResistToTemp(0);
                    }
                    else
                    {
                        //double r1 = Regular2Physical((ushort)pTarget.dbHexMin, pTarget.regref, pTarget.phyref)*1000 / current;
                        //double r2 = Regular2Physical((ushort)pTarget.dbHexMax, pTarget.regref, pTarget.phyref)*1000 / current;
                        double r1 = Formula.HexToR((ushort)pTarget.dbHexMin, pTarget.regref, pTarget.phyref);
                        double r2 = Formula.HexToR((ushort)pTarget.dbHexMax, pTarget.regref, pTarget.phyref);
                        double t1 = ResistToTemp(r1);
                        double t2 = ResistToTemp(r2);
                        if (t1 > t2)
                        {
                            pTarget.dbPhyMin = t2;
                            pTarget.dbPhyMax = t1;
                        }
                        else
                        {
                            pTarget.dbPhyMin = t1;
                            pTarget.dbPhyMax = t2;
                        }
                    }
                    break;
            }
            return;
        }

        /// <summary>
        /// 转换参数值类型从物理值到16进制值
        /// </summary>
        /// <param name="p"></param>
        /// <param name="relatedparameters"></param>
        public override void Physical2Hex(ref Parameter p)
        {
            UInt16 wdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            if (p == null) return;
            switch ((ElementDefine.SUBTYPE)p.subtype)
            {
                case ElementDefine.SUBTYPE.EXT_TEMP:
                    {
                        double r = TempToResist(p.phydata);
                        //double v = r / 1000.0 * Cref;
                        //wdata = Physical2Regular((float)v, p.regref, p.phyref);
                        wdata = Formula.RToHex(r, p.regref, p.phyref);
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

        public override void Hex2Physical(ref Parameter p)
        {
            UInt16 wdata = 0;
            double ddata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            if (p == null) return;
            switch ((ElementDefine.SUBTYPE)p.subtype)
            {
                case ElementDefine.SUBTYPE.EXT_TEMP:

                    ret = ReadFromRegImg(p, ref wdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    {
                        p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                        break;
                    }
                    //ddata = Regular2Physical(wdata, p.regref, p.phyref);
                    //ddata = ddata * 1000 / Cref;
                    ddata = Formula.HexToR(wdata, p.regref, p.phyref);
                    p.phydata = ResistToTemp(ddata);
                    break;
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
    }

    public static class Formula
    {
        public static double HexToR(ushort hex, double RegularRef, double PhysicalRef)
        {
            double ddata = Regular2Physical(hex, RegularRef, PhysicalRef);
            return ddata * 1000 / 20;
        }

        public static ushort RToHex(double R, double RegularRef, double PhysicalRef)
        {
            double v = R / 1000.0 * 20;

            return Physical2Regular((float)v, RegularRef, PhysicalRef);
        }
        /// <summary>
        /// 转换Hex -> Physical
        /// </summary>
        /// <param name="sVal"></param>
        /// <param name="RegularRef"></param>
        /// <param name="PhysicalRef"></param>
        /// <returns></returns>
        public static double Regular2Physical(UInt16 wVal, double RegularRef, double PhysicalRef)
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
        public static double Regular2Physical(short sVal, double RegularRef, double PhysicalRef)
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
        public static UInt16 Physical2Regular(float fVal, double RegularRef, double PhysicalRef)
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
    }
}
