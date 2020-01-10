using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.Caching;
using System.IO;

namespace CacheDemo
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        private MemoryCacheHelper cacheHelper;

        private void Form1_Load(object sender, EventArgs e)
        {            
            var cachePath = System.IO.Path.GetDirectoryName(Application.ExecutablePath) + "\\caches\\";
            var cacheFile = cachePath + "cachesData.data";
            CacheFileStore cacheStore = new CacheFileStore(cacheFile);
            cacheHelper = new MemoryCacheHelper(cacheStore, 10);
        
        }

        private void btnSet_Click(object sender, EventArgs e)
        {
            cacheHelper.SetData(tbKey.Text, tbValue.Text);
        }

        private void btnGet_Click(object sender, EventArgs e)
        {
            var val = cacheHelper.GetData(tbKey.Text);
            if (val != null)
            {
                tbValue.Text = val.ToString();
                return;
            }
            MessageBox.Show("未找到值");
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
          
        }
    }

   
}
