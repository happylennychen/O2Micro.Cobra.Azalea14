using O2Micro.Cobra.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace O2Micro.Cobra.Azalea14
{
    public static class CADCReader
    {
        internal static UInt32 ReadCADC(DEMBehaviorManageBase dem_base, ElementDefine.CADC_MODE mode)       //MP version new method. Do 4 time average by HW, and we can also have the trigger flag and coulomb counter work at the same time.
        {
            dem_base.parent.cadc_mode = mode;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            ushort temp = 0;
            switch (mode)
            {
                case ElementDefine.CADC_MODE.DISABLE:
                    #region disable
                    ret = dem_base.WriteWord(0x38, 0x00);        //clear all
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    #endregion
                    break;
                case ElementDefine.CADC_MODE.MOVING:
                    #region moving mode
                    ret = dem_base.ActiveModeCheck();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    bool cadc_moving_flag = false;
                    {
                        ret = dem_base.WriteWord(0x01, 0x0004);        //Clear cadc_moving_flag
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        ret = dem_base.WriteWord(0x38, 0x18);        //Set cc_always_enable, moving_average_enable, sw_cadc_ctrl=0b00
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        for (byte i = 0; i < ElementDefine.CADC_RETRY_COUNT; i++)
                        {
                            Thread.Sleep(30);
                            ret = dem_base.ReadWord(0x01, ref temp);
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
                            ret = dem_base.ReadWord(0x17, ref temp);
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

                    dem_base.parent.m_OpRegImg[0x17].err = ret;
                    dem_base.parent.m_OpRegImg[0x17].val = temp;
                    #endregion
                    break;
                case ElementDefine.CADC_MODE.TRIGGER:
                    #region trigger mode
                    ret = dem_base.ActiveModeCheck();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    bool cadc_trigger_flag = false;
                    {
                        ret = dem_base.WriteWord(0x01, 0x0002);        //Clear cadc_trigger_flag
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        ret = dem_base.WriteWord(0x38, 0x06);        //Set cadc_one_or_four, sw_cadc_ctrl=0b10
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        for (byte i = 0; i < ElementDefine.CADC_RETRY_COUNT; i++)
                        {
                            Thread.Sleep(60);
                            ret = dem_base.ReadWord(0x01, ref temp);
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
                            ret = dem_base.ReadWord(0x39, ref temp);
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

                    dem_base.parent.m_OpRegImg[0x39].err = ret;
                    dem_base.parent.m_OpRegImg[0x39].val = temp;
                    #endregion
                    break;
            }

            return ret;
        }
    }
}
