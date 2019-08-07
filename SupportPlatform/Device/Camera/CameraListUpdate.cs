using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data;

namespace SupportPlatform
{
    class CameraListUpdate
    {
        private delegate void CrossThreadOperationControl();
        public CameraListUpdate( DataTable dt)
        {           
            CrossThreadOperationControl CrossDele = delegate ()
            {
                Mainform.form1.kdtGrid_CameraList.DataSource = dt;
                //for (int k = 0; k < dt.Columns.Count; k++)
                //{
                //    Mainform.form1.kdtGrid_CameraList.Columns[k].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;//自适应下表单
                //}

            };
            Mainform.form1.kdtGrid_CameraList.Invoke(CrossDele);
        }
    }
}
