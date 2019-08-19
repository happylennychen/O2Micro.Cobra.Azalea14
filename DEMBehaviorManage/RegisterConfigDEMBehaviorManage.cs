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
    internal class RegisterConfigDEMBehaviorManage:DEMBehaviorManageBase
    {
        #region 基础服务功能设计
        public UInt32 RegisterConfig_ConvertHexToPhysical(ref TASKMessage msg)
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
                m_parent.RegisterConfig_Hex2Physical(ref param);
            }

            return ret;
        }
        public override UInt32 Command(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            switch ((ElementDefine.COMMAND)msg.sub_task)
            {
                case ElementDefine.COMMAND.REGISTER_CONFIG_WRITE_WITH_PASSWORD:
                    msg.percent = 20;
                    ret = GetRegisteInfor(ref msg);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    msg.percent = 30;
                    ret = Read(ref msg);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    msg.percent = 40;
                    ret = ConvertPhysicalToHex(ref msg);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;

                    msg.percent = 50;
                    ret = WriteWord(0x0f, 0x3714);      //password
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
                    ret = RegisterConfig_ConvertHexToPhysical(ref msg);
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
                case ElementDefine.COMMAND.REGISTER_CONFIG_READ:
                    {
                        msg.percent = 20;
                        ret = GetRegisteInfor(ref msg);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        msg.percent = 30;
                        ret = Read(ref msg);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        msg.percent = 80;
                        ret = RegisterConfig_ConvertHexToPhysical(ref msg);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
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
        #endregion
    }
}