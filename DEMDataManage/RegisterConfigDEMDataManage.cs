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
                        double current = 1;
                        if (parent.parent.pTHM_CRRT_SEL.phydata == 1)
                            current = 20;
                        else if(parent.parent.pTHM_CRRT_SEL.phydata == 2)
                            current = 120;
                        double r1 = (pTarget.dbHexMin * pTarget.phyref *1000 / pTarget.regref) / current;
                        double r2 = (pTarget.dbHexMax * pTarget.phyref * 1000 / pTarget.regref) / current;
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

        public override void Hex2Physical(ref Parameter p)
        {
            UInt16 wdata = 0;
            double ddata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            if (p == null) return;
            switch ((ElementDefine.SUBTYPE)p.subtype)
            {
                case ElementDefine.SUBTYPE.EXT_TEMP:
                    ushort Cref = 0;
                    switch (parent.parent.m_OpRegImg[0x11].val & 0x03)
                    {
                        case 0x01: Cref = 20; break;
                        case 0x02: Cref = 120; break;
                        default:break;
                    }

                    ret = ReadFromRegImg(p, ref wdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    {
                        p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                        break;
                    }
                    ddata = Regular2Physical(wdata, p.regref, p.phyref);
                    ddata = ddata * 1000 / Cref;
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
}
