using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using O2Micro.Cobra.Common;

namespace O2Micro.Cobra.Azalea14
{
    internal class SCSDEMDataManage:DEMDataManageBase
    {
        public SCSDEMDataManage(object pParent) : base(pParent)
        {
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
                        default: break;
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
