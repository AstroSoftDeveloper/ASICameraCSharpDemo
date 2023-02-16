using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZWOptical.ASISDK;
using System.Runtime.InteropServices;
using System.IO;
using System.Drawing.Imaging;

namespace ASICamera_demo
{
    public partial class Form1 : Form
    {
        // camera object
        private Camera m_camera = new Camera();
        // first Open
        private bool m_isFirstOpen = true;
        // last bin value
        private int m_iLastBin = 1;
        //private bool m_bCanSetValue = false;
        #region the callback of UI refresh delegation
        public void RefreshUI(Bitmap bmp)
        {
            if (this.InvokeRequired)
            {
                DisplayUICallback displayUI = new DisplayUICallback(DisplayUI);
                this.Invoke(displayUI, new object[] { bmp });
            }
            else
            {
                DisplayUI(bmp);
            }
        }
        private delegate void DisplayUICallback(Bitmap bmp);
        private void DisplayUI(Bitmap bmp)
        {
            pictureBox.Image = bmp;
            if (comboBox_captureMode.SelectedItem.ToString() == "Snap")
            {
                comboBox_captureMode.Enabled = true;
            }
        }

        public void PopupMessageBox(string str, int iVal)
        {
            if (this.InvokeRequired)
            {
                PopMessageBoxCallback PopupMessageBox = new PopMessageBoxCallback(_PopupMessageBox);
                this.Invoke(PopupMessageBox, new object[] { str , iVal });
            }
            else
            {
                _PopupMessageBox(str, iVal);
            }
        }
        private delegate void PopMessageBoxCallback(string str, int iVal);
        private void _PopupMessageBox(string str, int iVal)
        {
            if (str == "Get Temperature")
            {
                float fTemperature = (float)iVal / 10;
                label_temperature.Text = fTemperature.ToString() + "℃";

                return;
            }

            if (str == "Gain Auto")
            {
                trackBar_gain.Value = iVal;
                spinBox_gain.Value = iVal;
                return;
            }

            if (str == "Exposure Auto")
            {
                trackBar_exposure.Value = iVal;
                spinBox_exposure.Value = iVal;
                return;
            }

            if (str == "White Balance Blue Auto")
            {
                trackBar_WBB.Value = iVal;
                spinBox_WBB.Value = iVal;
                return;
            }

            if (str == "White Balance Red Auto")
            {
                trackBar_WBR.Value = iVal;
                spinBox_WBR.Value = iVal;
                return;
            }

            if (str == "BandWidth Auto")
            {
                trackBar_bandWidth.Value = iVal;
                spinBox_bandWidth.Value = iVal;
                return;
            }

            if (str == "No Camera Connection")
            {
                button_open.Enabled = false;
                label_cameraInfo.Visible = false;
                label_color.Visible = false;
                label_cooler.Visible = false;
                label_USB.Visible = false;
                label_USBHost.Visible = false;

                comboBox_cameraName.Items.Clear();
                comboBox_cameraName.Text = "";
            }

            MessageBox.Show(str);
        }

        #endregion
        // Constructor
        public Form1()
        {
            InitializeComponent();
            // Connect after opening the software
            string strCameraName = m_camera.scan();
            if (strCameraName != "")
            {
                comboBox_cameraName.Items.Add(strCameraName);
                comboBox_cameraName.SelectedIndex = 0;

                button_open.Enabled = true;
                label_cameraInfo.Visible = true;
                label_color.Visible = true;
                label_cooler.Visible = true;
                label_USB.Visible = true;
                label_USBHost.Visible = true;
                label_SN.Visible = true;
                label_temperature.Visible = true;

                if (m_camera.getIsColor()) label_color.Text = "Color"; else label_color.Text = "UnColor";
                if (m_camera.getIsCooler()) label_cooler.Text = "Cooler"; else label_cooler.Text = "UnCooler";
                if (m_camera.getIsUSB3()) label_USB.Text = "USB3Camera"; else label_USB.Text = "USB2Camera";
                if (m_camera.getIsUSB3Host()) label_USBHost.Text = "USB3Host"; else label_USBHost.Text = "USB2Host";
                if (m_camera.getIsUSB3Host()) label_USBHost.Text = "USB3Host"; else label_USBHost.Text = "USB2Host";
            }
            // Set the callback of UI refresh delegation
            m_camera.SetRefreshUICallBack(RefreshUI);
            m_camera.SetMessageBoxCallBack(PopupMessageBox);
        }
        // UI Init
        void UIInit()
        {
            // exposure time : unit us 32->10000
            int currentExpMs = m_camera.getCurrentExpMs();
            // 限制在1000000us，也就是1s
            if (currentExpMs >= 1000000)
                currentExpMs = 1000000;
            trackBar_exposure.Value = currentExpMs;
            spinBox_exposure.Value = currentExpMs;            
            // gain
            int maxGain = m_camera.getMaxGain();
            trackBar_gain.Maximum = maxGain;
            spinBox_gain.Maximum = maxGain;
            trackBar_gainLimit.Maximum = maxGain;
            spinBox_gainLimit.Maximum = maxGain;
            trackBar_gainLimit.Value = maxGain;
            spinBox_gainLimit.Value = maxGain;
            int currentGain = m_camera.getCurrentGain();
            trackBar_gain.Value = currentGain;
            spinBox_gain.Value = currentGain;

            int maxWBR = m_camera.getMaxWBR();
            trackBar_WBR.Maximum = maxWBR;
            spinBox_WBR.Maximum = maxWBR;
            int currentWBR = m_camera.getCurrentWBR();
            trackBar_WBR.Value = currentWBR;
            spinBox_WBR.Value = currentWBR;

            int maxWBB = m_camera.getMaxWBB();
            trackBar_WBB.Maximum = maxWBB;
            spinBox_WBB.Maximum = maxWBB;
            int currentWBB = m_camera.getCurrentWBB();
            trackBar_WBB.Value = currentWBB;
            spinBox_WBB.Value = currentWBB;

            int maxOffset = m_camera.getMaxOffset();
            trackBar_offset.Maximum = maxOffset;
            spinBox_offset.Maximum = maxOffset;
            trackBar_offsetLimit.Maximum = maxOffset;
            spinBox_offsetLimit.Maximum = maxOffset;
            int currentOffset = m_camera.getCurrentOffset();
            trackBar_offset.Value = currentOffset;
            spinBox_offset.Value = currentOffset;

            int currentBandWidth = m_camera.getCurrentBandWidth();
            trackBar_bandWidth.Value = currentBandWidth;
            spinBox_bandWidth.Value = currentBandWidth;

            comboBox_captureMode.Items.Clear();
            comboBox_captureMode.Items.Add("Video");
            comboBox_captureMode.Items.Add("Snap");
            comboBox_captureMode.SelectedIndex = 0;

            comboBox_flipType.Items.Clear();
            comboBox_flipType.Items.Add("None");
            comboBox_flipType.Items.Add("Horizon");
            comboBox_flipType.Items.Add("Vertical");
            comboBox_flipType.Items.Add("Both");
            comboBox_flipType.SelectedIndex = 0;

            comboBox_imageFormat.Items.Clear();
            ASICameraDll2.ASI_IMG_TYPE[] typeArr = m_camera.getImgTypeArr();
            int index = 0;
            while (typeArr[index] != ASICameraDll2.ASI_IMG_TYPE.ASI_IMG_END)
            {
                string[] list = typeArr[index].ToString().Split('_');
                comboBox_imageFormat.Items.Add(list[2]);
                index++;
            }

            comboBox_resolution.Items.Clear();
            comboBox_bin.Items.Clear();
            int[] binArr = m_camera.getBinArr();
            index = 0;
            while (binArr[index] != 0)
            {
                comboBox_bin.Items.Add("Bin" + binArr[index].ToString());
                int width = m_camera.getMaxWidth() / binArr[index];
                int height = m_camera.getMaxHeight() / binArr[index];
                // 向下圆整
                while (width % 8 != 0)
                {
                    width--;
                }
                while (height % 2 != 0)
                {
                    height--;
                }
                comboBox_resolution.Items.Add(width.ToString() + '*' + height.ToString());
                index++;
            }
            comboBox_imageFormat.SelectedIndex = 0;
            comboBox_bin.SelectedIndex = 0;
            comboBox_resolution.SelectedIndex = 0;

            spinBox_roiHeight.Maximum = m_camera.getMaxHeight();
            spinBox_roiWidth.Maximum = m_camera.getMaxWidth();
        }
        // refresh UI Enable
        private void refreshUIEnable(bool bEnable)
        {
            comboBox_bin.Enabled = bEnable;
            comboBox_captureMode.Enabled = bEnable;
            comboBox_imageFormat.Enabled = bEnable;
            comboBox_resolution.Enabled = bEnable;
            comboBox_flipType.Enabled = bEnable;

            button_close.Enabled = bEnable;
            button_scan.Enabled = !bEnable;
            button_open.Enabled = !bEnable;
            button_setROIFormat.Enabled = bEnable;

            spinBox_exposure.Enabled = bEnable;
            spinBox_gain.Enabled = bEnable;
            spinBox_roiHeight.Enabled = bEnable;
            spinBox_roiWidth.Enabled = bEnable;
            spinBox_startPosX.Enabled = bEnable;
            spinBox_startPosY.Enabled = bEnable;
            spinBox_WBR.Enabled = bEnable;
            spinBox_WBB.Enabled = bEnable;
            spinBox_bandWidth.Enabled = bEnable;
            spinBox_offset.Enabled = bEnable;
            spinBox_expLimit.Enabled = bEnable;
            spinBox_gainLimit.Enabled = bEnable;
            spinBox_offsetLimit.Enabled = bEnable;

            trackBar_exposure.Enabled = bEnable;
            trackBar_gain.Enabled = bEnable;
            trackBar_WBR.Enabled = bEnable;
            trackBar_WBB.Enabled = bEnable;
            trackBar_bandWidth.Enabled = bEnable;
            trackBar_offset.Enabled = bEnable;
            trackBar_expLimit.Enabled = bEnable;
            trackBar_gainLimit.Enabled = bEnable;
            trackBar_offsetLimit.Enabled = bEnable;

            checkBox_WBRAuto.Enabled = bEnable;
            checkBox_WBBAuto.Enabled = bEnable;
            checkBox_highSpeedMode.Enabled = bEnable;
            checkBox_monoBin.Enabled = bEnable;
            checkBox_gainAuto.Enabled = bEnable;
            checkBox_exposureAuto.Enabled = bEnable;
            checkBox_bandWidthAuto.Enabled = bEnable;
        }
        private void button_open_Click(object sender, EventArgs e)
        {
            if (m_camera.open())
            {
                refreshUIEnable(true);
                if (m_isFirstOpen)
                {
                    UIInit();
                    m_isFirstOpen = false;
                }
                if (comboBox_captureMode.SelectedItem.ToString() == "Video")
                {
                    button_startVideo.Enabled = true;
                    button_snap.Enabled = false;
                }
                else if (comboBox_captureMode.SelectedItem.ToString() == "Snap")
                {
                    button_startVideo.Enabled = false;
                    button_snap.Enabled = true;
                }                

                if(comboBox_captureMode.SelectedItem.ToString() == "Video")
                    startVideo();

                label_SN.Text = "SN: " + m_camera.getSN();

                gainAuto();
                exposureAuto();
                WBBAuto();
                WBRAuto();
                bandWidthAuto();
            }
        }
        private void button_close_Click(object sender, EventArgs e)
        {
            if (m_camera.close())
            {
                refreshUIEnable(false);
                button_startVideo.Enabled = false;
                button_snap.Enabled = false;
                button_startVideo.Text = "StartVideo";
            }
        }
        private void button_startVideo_Click(object sender, EventArgs e)
        {
            startVideo();
        }
        private void startVideo()
        {
            if (button_startVideo.Text == "StartVideo")
            {
                m_camera.startCapture();
                button_startVideo.Text = "StopVideo";
                comboBox_captureMode.Enabled = false;
            }
            else if (button_startVideo.Text == "StopVideo")
            {
                m_camera.stopCapture();
                button_startVideo.Text = "StartVideo";
                comboBox_captureMode.Enabled = true;
            }
        }
        private void refreshLabel()
        {

        }
        private void button_scan_Click(object sender, EventArgs e)
        {
            string strCameraName = m_camera.scan();
            if (strCameraName != "")
            {
                comboBox_cameraName.Items.Clear();
                comboBox_cameraName.Items.Add(strCameraName);
                comboBox_cameraName.SelectedIndex = 0;

                m_isFirstOpen = true;

                button_open.Enabled = true;
                label_cameraInfo.Visible = true;
                label_color.Visible = true;
                label_cooler.Visible = true;
                label_USB.Visible = true;
                label_USBHost.Visible = true;

                if (m_camera.getIsColor()) label_color.Text = "Color"; else label_color.Text = "UnColor";
                if (m_camera.getIsCooler()) label_cooler.Text = "Cooler"; else label_cooler.Text = "UnCooler";
                if (m_camera.getIsUSB3()) label_USB.Text = "USB3Camera"; else label_USB.Text = "USB2Camera";
                if (m_camera.getIsUSB3Host()) label_USBHost.Text = "USB3Host"; else label_USBHost.Text = "USB2Host";
            }
            else
            {
                button_open.Enabled = false;
                label_cameraInfo.Visible = false;
                label_color.Visible = false;
                label_cooler.Visible = false;
                label_USB.Visible = false;
                label_USBHost.Visible = false;

                comboBox_cameraName.Items.Clear();
                comboBox_cameraName.Text = "";
            }
        }
        private void button_setROIFormat_Click(object sender, EventArgs e)
        {
            int iWidth = (int)spinBox_roiWidth.Value;
            int iHeight = (int)spinBox_roiHeight.Value;
            int iStartPosX = (int)spinBox_startPosX.Value;
            int iStartPosY = (int)spinBox_startPosY.Value;

            string strType = strType = comboBox_imageFormat.SelectedItem.ToString();
            int iBin = (int)Char.GetNumericValue(comboBox_bin.Text.Last());

            m_camera.setImageFormat(iWidth, iHeight, iStartPosX, iStartPosY, iBin, str2Type(strType));

            int index = comboBox_resolution.Items.IndexOf(iWidth.ToString() + '*' + iHeight.ToString());
            if (index != -1)
            {
                comboBox_resolution.SelectedIndex = index;
            }
            else
            {
                string strResolution = iWidth.ToString() + '*' + iHeight.ToString();
                comboBox_resolution.Items.Add(strResolution);
                index = comboBox_resolution.Items.IndexOf(strResolution);
                comboBox_resolution.SelectedIndex = index;
            }
        }
        private void button_snap_Click(object sender, EventArgs e)
        {
            m_camera.startCapture();
            comboBox_captureMode.Enabled = false;
        }
        private void comboBox_captureMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox_captureMode.SelectedItem.ToString() == "Video")
            {
                m_camera.switchMode(Camera.CaptureMode.Video);
                button_startVideo.Enabled = true;
                button_snap.Enabled = false;
            }
            else if (comboBox_captureMode.SelectedItem.ToString() == "Snap")
            {
                m_camera.switchMode(Camera.CaptureMode.Snap);
                button_startVideo.Enabled = false;
                button_snap.Enabled = true;
            }
        }
        private void trackBar_gain_Scroll(object sender, EventArgs e)
        {
            if (!trackBar_gain.Enabled)
                return;

            int val = trackBar_gain.Value;
            if (m_camera.setControlValue(ASICameraDll2.ASI_CONTROL_TYPE.ASI_GAIN, val, ASICameraDll2.ASI_BOOL.ASI_FALSE))
            {
                spinBox_gain.Value = val;
            }
        }
        private void spinBox_gain_ValueChanged(object sender, EventArgs e)
        {
            if (!spinBox_gain.Enabled)
                return;

            int val = (int)spinBox_gain.Value;
            if (m_camera.setControlValue(ASICameraDll2.ASI_CONTROL_TYPE.ASI_GAIN, val, ASICameraDll2.ASI_BOOL.ASI_FALSE))
            {
                trackBar_gain.Value = val;
            }
        }

        public void getFormatParas(out string strType, out int iWidth, out int iHeight, out int iBin)
        {
            if(m_isFirstOpen)
            {
                strType = "RAW8";
                iWidth = m_camera.getMaxWidth();
                iHeight = m_camera.getMaxHeight();
                iBin = 1;
            }
            else
            {
                strType = comboBox_imageFormat.SelectedItem.ToString();

                string[] list = comboBox_resolution.SelectedItem.ToString().Split('*');
                iWidth = Convert.ToInt32(list[0]);
                iHeight = Convert.ToInt32(list[1]);

                iBin = (int)Char.GetNumericValue(comboBox_bin.Text.Last());
            }
            m_iLastBin = iBin;
        }
        private void comboBox_imageFormat_SelectedIndexChanged(object sender, EventArgs e)
        {
            string strType = "";
            int iBin = 0;
            int iWidth = 0;
            int iHeight = 0;
            getFormatParas(out strType, out iWidth, out iHeight, out iBin);

            m_camera.setImageFormat(iWidth, iHeight, 0, 0, iBin, str2Type(strType));
        }
        private void comboBox_bin_SelectedIndexChanged(object sender, EventArgs e)
        {
            string strType = "";
            int iBin = 0;
            int iWidth = 0;
            int iHeight = 0;

            int iLastBin = m_iLastBin;
            getFormatParas(out strType, out iWidth, out iHeight, out iBin);
            int iCurBin = m_iLastBin;
            float fRatio = (float)iLastBin / (float)iCurBin;
            float fWidth = (float)iWidth * fRatio;
            float fHeight = (float)iHeight * fRatio;
            iWidth = (int)fWidth;
            iHeight = (int)fHeight;

            // 向下圆整
            while(iWidth%8 != 0)
            {
                iWidth--;
            }
            while (iHeight % 2 != 0)
            {
                iHeight--;
            }

            m_camera.setImageFormat(iWidth, iHeight, 0, 0, iBin, str2Type(strType));

            int index = comboBox_resolution.Items.IndexOf(iWidth.ToString() + '*' + iHeight.ToString());
            if(index != -1)
            {
                comboBox_resolution.SelectedIndex = index;
            }
            else
            {
                string strResolution = iWidth.ToString() + '*' + iHeight.ToString();
                comboBox_resolution.Items.Add(strResolution);
                index = comboBox_resolution.Items.IndexOf(strResolution);
                comboBox_resolution.SelectedIndex = index;
            }            
        }
        private void comboBox_resolution_SelectedIndexChanged(object sender, EventArgs e)
        {
            string strType = "";
            int iBin = 0;
            int iWidth = 0;
            int iHeight = 0;
            getFormatParas(out strType, out iWidth, out iHeight, out iBin);

            m_camera.setImageFormat(iWidth, iHeight, 0, 0, iBin, str2Type(strType));
        }
        // method
        public ASICameraDll2.ASI_IMG_TYPE str2Type(string strType)
        {
            if (strType == "RAW8")
            {
                return ASICameraDll2.ASI_IMG_TYPE.ASI_IMG_RAW8;
            }
            else if (strType == "RAW16")
            {
                return ASICameraDll2.ASI_IMG_TYPE.ASI_IMG_RAW16;
            }
            else if (strType == "RGB24")
            {
                return ASICameraDll2.ASI_IMG_TYPE.ASI_IMG_RGB24;
            }
            else
            {
                return ASICameraDll2.ASI_IMG_TYPE.ASI_IMG_Y8;
            }
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            m_camera.close();
            m_camera.exitCaptureThread();
        }
        private void gainAuto()
        {
            int val = trackBar_gain.Value;

            if (!checkBox_gainAuto.Checked)
            {
                if (m_camera.setControlValueAuto(ASICameraDll2.ASI_CONTROL_TYPE.ASI_GAIN, val, ASICameraDll2.ASI_BOOL.ASI_FALSE))
                {
                    trackBar_gain.Enabled = true;
                    spinBox_gain.Enabled = true;
                }
            }
            else
            {
                if (m_camera.setControlValueAuto(ASICameraDll2.ASI_CONTROL_TYPE.ASI_GAIN, val, ASICameraDll2.ASI_BOOL.ASI_TRUE))
                {
                    trackBar_gain.Enabled = false;
                    spinBox_gain.Enabled = false;
                }
            }
        }
        private void checkBox_gainAuto_CheckedChanged(object sender, EventArgs e)
        {
            gainAuto();
        }
        private void exposureAuto()
        {
            int val = trackBar_exposure.Value;

            if (!checkBox_exposureAuto.Checked)
            {
                if (m_camera.setControlValueAuto(ASICameraDll2.ASI_CONTROL_TYPE.ASI_EXPOSURE, val, ASICameraDll2.ASI_BOOL.ASI_FALSE))
                {
                    trackBar_exposure.Enabled = true;
                    spinBox_exposure.Enabled = true;
                }
            }
            else
            {
                if (m_camera.setControlValueAuto(ASICameraDll2.ASI_CONTROL_TYPE.ASI_EXPOSURE, val, ASICameraDll2.ASI_BOOL.ASI_TRUE))
                {
                    trackBar_exposure.Enabled = false;
                    spinBox_exposure.Enabled = false;
                }
            }
        }
        private void checkBox_ExpAuto_CheckedChanged(object sender, EventArgs e)
        {
            exposureAuto();
        }
        private void trackBar_exposure_Scroll(object sender, EventArgs e)
        {
            if (!trackBar_gain.Enabled)
                return;

            int val = trackBar_exposure.Value;
            if (m_camera.setControlValue(ASICameraDll2.ASI_CONTROL_TYPE.ASI_EXPOSURE, val, ASICameraDll2.ASI_BOOL.ASI_FALSE))
            {
                spinBox_exposure.Value = val;
            }
        }
        private void spinBox_exposure_ValueChanged(object sender, EventArgs e)
        {
            if (!spinBox_exposure.Enabled)
                return;

            int val = (int)spinBox_exposure.Value;
            if (m_camera.setControlValue(ASICameraDll2.ASI_CONTROL_TYPE.ASI_EXPOSURE, val, ASICameraDll2.ASI_BOOL.ASI_FALSE))
            {
                trackBar_exposure.Value = val;
            }
        }
        private void trackBar_WBR_Scroll(object sender, EventArgs e)
        {
            if (!trackBar_WBR.Enabled)
                return;

            int val = trackBar_WBR.Value;
            if (m_camera.setControlValue(ASICameraDll2.ASI_CONTROL_TYPE.ASI_WB_R, val, ASICameraDll2.ASI_BOOL.ASI_FALSE))
            {
                spinBox_WBR.Value = val;
            }
        }

        private void spinBox_WBR_ValueChanged(object sender, EventArgs e)
        {
            if (!spinBox_WBR.Enabled)
                return;

            int val = (int)spinBox_WBR.Value;
            if (m_camera.setControlValue(ASICameraDll2.ASI_CONTROL_TYPE.ASI_WB_R, val, ASICameraDll2.ASI_BOOL.ASI_FALSE))
            {
                trackBar_WBR.Value = val;
            }
        }
        private void WBRAuto()
        {
            int val = trackBar_WBR.Value;

            if (!checkBox_WBRAuto.Checked)
            {
                if (m_camera.setControlValueAuto(ASICameraDll2.ASI_CONTROL_TYPE.ASI_WB_R, val, ASICameraDll2.ASI_BOOL.ASI_FALSE))
                {
                    trackBar_WBR.Enabled = true;
                    spinBox_WBR.Enabled = true;
                    checkBox_WBBAuto.Checked = false;
                }
            }
            else
            {
                if (m_camera.setControlValueAuto(ASICameraDll2.ASI_CONTROL_TYPE.ASI_WB_R, val, ASICameraDll2.ASI_BOOL.ASI_TRUE))
                {
                    trackBar_WBR.Enabled = false;
                    spinBox_WBR.Enabled = false;
                    checkBox_WBBAuto.Checked = true;
                }
            }

        }
        private void checkBox_WBRAuto_CheckedChanged(object sender, EventArgs e)
        {
            WBRAuto();
        }
        private void trackBar_WBB_Scroll(object sender, EventArgs e)
        {
            if (!trackBar_WBB.Enabled)
                return;

            int val = trackBar_WBB.Value;
            if (m_camera.setControlValue(ASICameraDll2.ASI_CONTROL_TYPE.ASI_WB_B, val, ASICameraDll2.ASI_BOOL.ASI_FALSE))
            {
                spinBox_WBB.Value = val;
            }
        }
        private void spinBox_WBB_ValueChanged(object sender, EventArgs e)
        {
            if (!spinBox_WBB.Enabled)
                return;

            int val = (int)spinBox_WBB.Value;
            if (m_camera.setControlValue(ASICameraDll2.ASI_CONTROL_TYPE.ASI_WB_B, val, ASICameraDll2.ASI_BOOL.ASI_FALSE))
            {
                trackBar_WBB.Value = val;
            }
        }
        private void WBBAuto()
        {
            int val = trackBar_WBB.Value;

            if (!checkBox_WBBAuto.Checked)
            {
                if (m_camera.setControlValueAuto(ASICameraDll2.ASI_CONTROL_TYPE.ASI_WB_B, val, ASICameraDll2.ASI_BOOL.ASI_FALSE))
                {
                    trackBar_WBB.Enabled = true;
                    spinBox_WBB.Enabled = true;
                    checkBox_WBRAuto.Checked = false;
                }
            }
            else
            {
                if (m_camera.setControlValueAuto(ASICameraDll2.ASI_CONTROL_TYPE.ASI_WB_B, val, ASICameraDll2.ASI_BOOL.ASI_TRUE))
                {
                    trackBar_WBB.Enabled = false;
                    spinBox_WBB.Enabled = false;
                    checkBox_WBRAuto.Checked = true;
                }
            }

        }
        private void checkBox_WBBAuto_CheckedChanged(object sender, EventArgs e)
        {
            WBBAuto();
        }

        private void trackBar_offset_Scroll(object sender, EventArgs e)
        {
            int val = trackBar_offset.Value;
            if (m_camera.setControlValue(ASICameraDll2.ASI_CONTROL_TYPE.ASI_OFFSET, val, ASICameraDll2.ASI_BOOL.ASI_FALSE))
            {
                spinBox_offset.Value = val;
            }
        }

        private void spinBox_offset_ValueChanged(object sender, EventArgs e)
        {
            int val = (int)spinBox_offset.Value;
            if (m_camera.setControlValue(ASICameraDll2.ASI_CONTROL_TYPE.ASI_OFFSET, val, ASICameraDll2.ASI_BOOL.ASI_FALSE))
            {
                trackBar_offset.Value = val;
            }
        }

        private void trackBar_bandWidth_Scroll(object sender, EventArgs e)
        {
            if (!trackBar_bandWidth.Enabled)
                return;

            int val = trackBar_bandWidth.Value;
            if (m_camera.setControlValue(ASICameraDll2.ASI_CONTROL_TYPE.ASI_BANDWIDTHOVERLOAD, val, ASICameraDll2.ASI_BOOL.ASI_FALSE))
            {
                spinBox_bandWidth.Value = val;
            }
        }

        private void spinBox_bandWidth_ValueChanged(object sender, EventArgs e)
        {
            if (!spinBox_bandWidth.Enabled)
                return;

            int val = (int)spinBox_bandWidth.Value;
            if (m_camera.setControlValue(ASICameraDll2.ASI_CONTROL_TYPE.ASI_BANDWIDTHOVERLOAD, val, ASICameraDll2.ASI_BOOL.ASI_FALSE))
            {
                trackBar_bandWidth.Value = val;
            }
        }
        private void bandWidthAuto()
        {
            int val = trackBar_bandWidth.Value;

            if (!checkBox_bandWidthAuto.Checked)
            {
                if (m_camera.setControlValueAuto(ASICameraDll2.ASI_CONTROL_TYPE.ASI_BANDWIDTHOVERLOAD, val, ASICameraDll2.ASI_BOOL.ASI_FALSE))
                {
                    trackBar_bandWidth.Enabled = true;
                    spinBox_bandWidth.Enabled = true;
                }
            }
            else
            {
                if (m_camera.setControlValueAuto(ASICameraDll2.ASI_CONTROL_TYPE.ASI_BANDWIDTHOVERLOAD, val, ASICameraDll2.ASI_BOOL.ASI_TRUE))
                {
                    trackBar_bandWidth.Enabled = false;
                    spinBox_bandWidth.Enabled = false;
                }
            }

        }
        private void checkBox_bandWidthAuto_CheckedChanged(object sender, EventArgs e)
        {
            bandWidthAuto();
        }

        private void checkBox_highSpeedMode_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBox_highSpeedMode.Checked)
            {
                m_camera.setControlValue(ASICameraDll2.ASI_CONTROL_TYPE.ASI_HIGH_SPEED_MODE, 0, ASICameraDll2.ASI_BOOL.ASI_FALSE);
            }
            else
            {
                m_camera.setControlValue(ASICameraDll2.ASI_CONTROL_TYPE.ASI_HIGH_SPEED_MODE, 1, ASICameraDll2.ASI_BOOL.ASI_FALSE);
            }
        }

        private void checkBox_monoBin_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBox_monoBin.Checked)
            {
                m_camera.setControlValue(ASICameraDll2.ASI_CONTROL_TYPE.ASI_MONO_BIN, 0, ASICameraDll2.ASI_BOOL.ASI_FALSE);
            }
            else
            {
                m_camera.setControlValue(ASICameraDll2.ASI_CONTROL_TYPE.ASI_MONO_BIN, 1, ASICameraDll2.ASI_BOOL.ASI_FALSE);
            }
        }

        private void comboBox_flipType_SelectedIndexChanged(object sender, EventArgs e)
        {
            // 0 none 1 horizon 2 vertical 3 both
            int val = comboBox_flipType.SelectedIndex;
            m_camera.setControlValue(ASICameraDll2.ASI_CONTROL_TYPE.ASI_FLIP, val, ASICameraDll2.ASI_BOOL.ASI_FALSE);
        }

        private void trackBar_gainLimit_Scroll(object sender, EventArgs e)
        {
            if (!trackBar_gainLimit.Enabled)
                return;

            int val = trackBar_gainLimit.Value;
            if (m_camera.setControlValue(ASICameraDll2.ASI_CONTROL_TYPE.ASI_AUTO_MAX_GAIN, val, ASICameraDll2.ASI_BOOL.ASI_FALSE))
            {
                spinBox_gainLimit.Value = val;
            }
        }

        private void spinBox_gainLimit_ValueChanged(object sender, EventArgs e)
        {
            if (!spinBox_gainLimit.Enabled)
                return;

            int val = (int)spinBox_gainLimit.Value;
            if (m_camera.setControlValue(ASICameraDll2.ASI_CONTROL_TYPE.ASI_AUTO_MAX_GAIN, val, ASICameraDll2.ASI_BOOL.ASI_FALSE))
            {
                trackBar_gainLimit.Value = val;
            }
        }

        private void trackBar_expLimit_Scroll(object sender, EventArgs e)
        {
            if (!trackBar_expLimit.Enabled)
                return;

            int val = trackBar_expLimit.Value;
            if (m_camera.setControlValue(ASICameraDll2.ASI_CONTROL_TYPE.ASI_AUTO_MAX_EXP, val, ASICameraDll2.ASI_BOOL.ASI_FALSE))
            {
                spinBox_expLimit.Value = val;
            }
        }

        private void spinBox_expLimit_ValueChanged(object sender, EventArgs e)
        {
            if (!spinBox_expLimit.Enabled)
                return;

            int val = (int)spinBox_expLimit.Value;
            if (m_camera.setControlValue(ASICameraDll2.ASI_CONTROL_TYPE.ASI_AUTO_MAX_EXP, val, ASICameraDll2.ASI_BOOL.ASI_FALSE))
            {
                trackBar_expLimit.Value = val;
            }
        }

        private void trackBar_offsetLimit_Scroll(object sender, EventArgs e)
        {
            if (!trackBar_offsetLimit.Enabled)
                return;

            int val = trackBar_offsetLimit.Value;
            if (m_camera.setControlValue(ASICameraDll2.ASI_CONTROL_TYPE.ASI_AUTO_MAX_BRIGHTNESS, val, ASICameraDll2.ASI_BOOL.ASI_FALSE))
            {
                spinBox_offsetLimit.Value = val;
            }
        }

        private void spinBox_offsetLimit_ValueChanged(object sender, EventArgs e)
        {
            if (!spinBox_offsetLimit.Enabled)
                return;

            int val = (int)spinBox_offsetLimit.Value;
            if (m_camera.setControlValue(ASICameraDll2.ASI_CONTROL_TYPE.ASI_AUTO_MAX_BRIGHTNESS, val, ASICameraDll2.ASI_BOOL.ASI_FALSE))
            {
                trackBar_offsetLimit.Value = val;
            }
        }
    }
}
