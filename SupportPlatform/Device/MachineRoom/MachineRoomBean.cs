using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupportPlatform.Device.MachineRoom
{
    class MachineRoomBean
    {
        /// <summary>
        /// 机房数据bean
        /// </summary>
        private string[] strAirsql;  //获取空调温湿度
        private string[] strUPSsql;  //获取UDP数据
        private List<string> strLisSerRoomsql = new List<string>();//获取 机房水浸、温湿度、消防
        private string strUPSType; //UPS类型
        private string strPowerUpType;//供电模式
        private string strWorkType; //工作状态   
        private string strElectricType;//市电状态   
        private string strOutputType; //输出状态        
        private string strCellType;//电池状态
        private string strMachineRoomName;//电池状态

        public string[] StrAirsql { get => strAirsql; set => strAirsql = value; }
        public string[] StrUPSsql { get => strUPSsql; set => strUPSsql = value; }
        public List<string> StrLisSerRoomsql { get => strLisSerRoomsql; set => strLisSerRoomsql = value; }
        public string StrUPSType { get => strUPSType; set => strUPSType = value; }
        public string StrPowerUpType { get => strPowerUpType; set => strPowerUpType = value; }
        public string StrWorkType { get => strWorkType; set => strWorkType = value; }
        public string StrElectricType { get => strElectricType; set => strElectricType = value; }
        public string StrOutputType { get => strOutputType; set => strOutputType = value; }
        public string StrCellType { get => strCellType; set => strCellType = value; }
        public string StrMachineRoomName { get => strMachineRoomName; set => strMachineRoomName = value; }

        public MachineRoomBean()
        {
            
        }
    }
}
