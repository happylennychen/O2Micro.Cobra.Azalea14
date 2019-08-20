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
    internal class SCSDEMBehaviorManage:DEMBehaviorManageBase
    {

        #region 基础服务功能设计
        public override UInt32 Command(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            switch ((ElementDefine.COMMAND)msg.sub_task)
            {
                case ElementDefine.COMMAND.SCS:
                    if (msg.task_parameterlist.parameterlist[0].guid == ElementDefine.TRIGGER_CADC)
                    {
                        ret = CADCReader.ReadCADC(this, ElementDefine.CADC_MODE.TRIGGER);
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
            }
            return ret;
        }
        #endregion
    }
}