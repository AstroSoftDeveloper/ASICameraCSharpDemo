using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using ZWOptical.ASISDK;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;

namespace ASICamera_demo
{
	class Camera
	{
		public enum CaptureMode
		{
			Video = 0,
			Snap = 1
		};
		private string m_cameraName = "";
		private ASICameraDll2.ASI_IMG_TYPE m_imgType = ASICameraDll2.ASI_IMG_TYPE.ASI_IMG_RAW8;
		private ASICameraDll2.ASI_SN m_SN;
		private CaptureMode m_CaptureMode = CaptureMode.Video;
		private int m_iCameraID;
		private int m_iMaxWidth;
		private int m_iMaxHeight;
		private int m_iCurWidth;
		private int m_iCurHeight;
		private int m_iSize;
		private int m_iBin;
		private int[] m_supBins = new int[16];
		private ASICameraDll2.ASI_IMG_TYPE[] m_supVideoFormats = new ASICameraDll2.ASI_IMG_TYPE[8];
		private int m_iCurrentGainValue;
		private int m_iCurrentExpMs;
		private int m_iCurrentWBR;
		private int m_iCurrentWBB;
		private int m_iCurrentBandWidth;
		//private int m_iTemperature;
		private int m_iCurrentOffset;
		private int m_iMaxGainValue;
		private int m_iMaxWBRValue;
		private int m_iMaxWBBValue;
		private int m_iMaxOffset;
		private bool m_bIsOpen = false;
		private bool m_bIsColor = false;
		private bool m_bIsCooler = false;
		private bool m_bIsUSB3 = false;
		private bool m_bIsUSB3Host = false;

		private bool m_bGainAutoChecked = false;
		private bool m_bExposureAutoChecked = false;
		private bool m_bWhiteBalanceAutoChecked = false;
		private bool m_bBandWidthAutoChecked = false;

		private System.Timers.Timer m_timer = new System.Timers.Timer(500);// 实例化Timer类，设置间隔时间为1000毫秒
		Thread captureThread;
		public string getSN()
		{
			return m_SN.SN;
		}
		public int getCurrentExpMs()
        {
			return m_iCurrentExpMs;
		}
		public int getCurrentGain()
		{
			return m_iCurrentGainValue;
		}
		public int getCurrentWBR()
		{
			return m_iCurrentWBR;
		}
		public int getCurrentWBB()
		{
			return m_iCurrentWBB;
		}
		public int getCurrentBandWidth()
		{
			return m_iCurrentBandWidth;
		}
		public int getCurrentOffset()
		{
			return m_iCurrentOffset;
		}
		public int getMaxOffset()
		{
			return m_iMaxOffset;
		}
		public int getMaxGain()
		{
			return m_iMaxGainValue;
		}
		public int getMaxWBR()
		{
			return m_iMaxWBRValue;
		}
		public int getMaxWBB()
		{
			return m_iMaxWBBValue;
		}
		public int getMaxWidth()
		{
			return m_iMaxWidth;
		}
		public int getMaxHeight()
		{
			return m_iMaxHeight;
		}
		public bool getIsColor()
		{
			return m_bIsColor;
		}
		public bool getIsCooler()
		{
			return m_bIsCooler;
		}
		public bool getIsUSB3()
		{
			return m_bIsUSB3;
		}
		public bool getIsUSB3Host()
		{
			return m_bIsUSB3Host;
		}
		public int[] getBinArr()
        {
			return m_supBins;
		}
		public ASICameraDll2.ASI_IMG_TYPE[] getImgTypeArr()
		{
			return m_supVideoFormats;
		}
		// Constructor
		public Camera()
		{
			captureThread = new Thread(new ThreadStart(run));
			m_timer.Elapsed += new System.Timers.ElapsedEventHandler(timeout); // 到达时间的时候执行事件
			m_timer.AutoReset = true;   // 设置是执行一次（false）还是一直执行(true)
			m_timer.Start();
		}
		private void timeout(object source, System.Timers.ElapsedEventArgs e)
		{
			if (!m_bIsOpen)
				return;

			int iVal = 0;

			ASICameraDll2.ASIGetControlValue(m_iCameraID, ASICameraDll2.ASI_CONTROL_TYPE.ASI_TEMPERATURE, out iVal);
			PopupMessageBox("Get Temperature", iVal);

			if (m_bGainAutoChecked)
            {
				if(getControlValue(ASICameraDll2.ASI_CONTROL_TYPE.ASI_GAIN, out iVal))
                {
					PopupMessageBox("Gain Auto", iVal);
                }
            }
			if (m_bExposureAutoChecked)
			{
				if (getControlValue(ASICameraDll2.ASI_CONTROL_TYPE.ASI_EXPOSURE, out iVal))
				{
					PopupMessageBox("Exposure Auto", iVal);
				}
			}

			if (m_bWhiteBalanceAutoChecked)
			{
				if (getControlValue(ASICameraDll2.ASI_CONTROL_TYPE.ASI_WB_B, out iVal))
				{
					PopupMessageBox("White Balance Blue Auto", iVal);
				}
				if (getControlValue(ASICameraDll2.ASI_CONTROL_TYPE.ASI_WB_R, out iVal))
				{
					PopupMessageBox("White Balance Red Auto", iVal);
				}
			}
			if (m_bBandWidthAutoChecked)
			{
				if (getControlValue(ASICameraDll2.ASI_CONTROL_TYPE.ASI_BANDWIDTHOVERLOAD, out iVal))
				{
					PopupMessageBox("BandWidth Auto", iVal);
				}
			}
		}
		// camera Init
		private bool cameraInit()
		{
			ASICameraDll2.ASI_ERROR_CODE err;
			int cameraNum = ASICameraDll2.ASIGetNumOfConnectedCameras();
			if (cameraNum == 0)
			{
				PopupMessageBox("No Camera Connection");
				return false;
			}

			ASICameraDll2.ASI_CAMERA_INFO CamInfoTemp;
			ASICameraDll2.ASIGetCameraProperty(out CamInfoTemp, 0);

			for (int i = 0; i < 16; i++)
			{
				m_supBins[i] = 0;
			}
			int index = 0;
			while (CamInfoTemp.SupportedBins[index] != 0)
            {
                m_supBins[index] = CamInfoTemp.SupportedBins[index];
                index++;
            }

			for (int i = 0; i < 8; i++)
			{
				m_supVideoFormats[i] = ASICameraDll2.ASI_IMG_TYPE.ASI_IMG_END;
			}
			index = 0;
            while (CamInfoTemp.SupportedVideoFormat[index] != ASICameraDll2.ASI_IMG_TYPE.ASI_IMG_END)
            {
                m_supVideoFormats[index] = CamInfoTemp.SupportedVideoFormat[index];
                index++;
            }

            err = ASICameraDll2.ASIOpenCamera(m_iCameraID);
			if (err != ASICameraDll2.ASI_ERROR_CODE.ASI_SUCCESS)
			{
				return false;
			}

			err = ASICameraDll2.ASIInitCamera(m_iCameraID);
			if (err != ASICameraDll2.ASI_ERROR_CODE.ASI_SUCCESS)
			{
				return false;
			}

			int iCtrlNum;
			ASICameraDll2.ASI_CONTROL_CAPS CtrlCap;
			ASICameraDll2.ASIGetNumOfControls(m_iCameraID, out iCtrlNum);

			for (int i = 0; i < iCtrlNum; i++)
			{
				ASICameraDll2.ASIGetControlCaps(m_iCameraID, i, out CtrlCap);
				if (CtrlCap.ControlType == ASICameraDll2.ASI_CONTROL_TYPE.ASI_GAIN)
				{
					m_iMaxGainValue = CtrlCap.MaxValue;
				}
				else if (CtrlCap.ControlType == ASICameraDll2.ASI_CONTROL_TYPE.ASI_WB_R)
                {
					m_iMaxWBRValue = CtrlCap.MaxValue;
				}
				else if (CtrlCap.ControlType == ASICameraDll2.ASI_CONTROL_TYPE.ASI_WB_B)
				{
					m_iMaxWBBValue = CtrlCap.MaxValue;
				}
				else if (CtrlCap.ControlType == ASICameraDll2.ASI_CONTROL_TYPE.ASI_OFFSET)
				{
					m_iMaxOffset = CtrlCap.MaxValue;
				}
			}

			ASICameraDll2.ASIGetControlValue(m_iCameraID, ASICameraDll2.ASI_CONTROL_TYPE.ASI_GAIN, out m_iCurrentGainValue);
			ASICameraDll2.ASIGetControlValue(m_iCameraID, ASICameraDll2.ASI_CONTROL_TYPE.ASI_EXPOSURE, out m_iCurrentExpMs);
			ASICameraDll2.ASIGetControlValue(m_iCameraID, ASICameraDll2.ASI_CONTROL_TYPE.ASI_WB_B, out m_iCurrentWBB);
			ASICameraDll2.ASIGetControlValue(m_iCameraID, ASICameraDll2.ASI_CONTROL_TYPE.ASI_WB_R, out m_iCurrentWBR);
			ASICameraDll2.ASIGetControlValue(m_iCameraID, ASICameraDll2.ASI_CONTROL_TYPE.ASI_OFFSET, out m_iCurrentOffset);
			ASICameraDll2.ASIGetControlValue(m_iCameraID, ASICameraDll2.ASI_CONTROL_TYPE.ASI_BANDWIDTHOVERLOAD, out m_iCurrentBandWidth);
			//ASICameraDll2.ASIGetControlValue(m_iCameraID, ASICameraDll2.ASI_CONTROL_TYPE.ASI_TEMPERATURE, out m_iTemperature);
			ASICameraDll2.ASIGetSerialNumber(m_iCameraID, out m_SN);

			int startx = 0, starty = 0;
			m_iBin = 1;
			err = ASICameraDll2.ASISetROIFormat(m_iCameraID, CamInfoTemp.MaxWidth, CamInfoTemp.MaxHeight, m_iBin, m_imgType);
			if (err != ASICameraDll2.ASI_ERROR_CODE.ASI_SUCCESS)
			{
				return false;
			}
			else
			{
				ASICameraDll2.ASISetStartPos(m_iCameraID, startx, starty);
				ASICameraDll2.ASIGetStartPos(m_iCameraID, out startx, out starty);
			}

			return true;
		}
		public bool open()
		{
			if (!cameraInit())
			{
				m_bIsOpen = false;
				return false;
			}
			m_bIsOpen = true;
			return true;
		}
		public bool close()
		{
			ASICameraDll2.ASI_ERROR_CODE err = ASICameraDll2.ASICloseCamera(m_iCameraID);
			if (err != ASICameraDll2.ASI_ERROR_CODE.ASI_SUCCESS)
				return false;
			stopCapture();
			m_bIsOpen = false;
			return true;
		}
		public void startCapture()
		{
			if (!m_bIsOpen)
				return;

			if (m_CaptureMode == CaptureMode.Video)
			{
				ASICameraDll2.ASIStartVideoCapture(m_iCameraID);
				startCaptureThread();
			}
			else if (m_CaptureMode == CaptureMode.Snap)
			{
				startCaptureThread();
			}
		}
		public void stopCapture()
		{
			if (!m_bIsOpen)
				return;			
			if (m_CaptureMode == CaptureMode.Video)
			{
				stopCaptureThread();
				ASICameraDll2.ASIStopVideoCapture(m_iCameraID);
			}
			else if (m_CaptureMode == CaptureMode.Snap)
            {
				stopCaptureThread();
			}
		}
		public void switchMode(CaptureMode mode)
        {
			m_CaptureMode = mode;
        }
		public bool setControlValue(ASICameraDll2.ASI_CONTROL_TYPE type, int value, ASICameraDll2.ASI_BOOL bAuto)
		{
			ASICameraDll2.ASI_ERROR_CODE err = ASICameraDll2.ASISetControlValue(m_iCameraID, type, value, bAuto);
			if(err == ASICameraDll2.ASI_ERROR_CODE.ASI_SUCCESS)
			{
				return true;
            }
            else
            {
				PopupMessageBox("Set Control Value Fail: " + err.ToString());
				return false;
            }
		}
		public bool setControlValueAuto(ASICameraDll2.ASI_CONTROL_TYPE type, int value, ASICameraDll2.ASI_BOOL bAuto)
		{
			ASICameraDll2.ASI_ERROR_CODE err = ASICameraDll2.ASISetControlValue(m_iCameraID, type, value, bAuto);
			if (err == ASICameraDll2.ASI_ERROR_CODE.ASI_SUCCESS)
			{
				if (bAuto == ASICameraDll2.ASI_BOOL.ASI_TRUE)
				{
					switch (type)
					{
						case ASICameraDll2.ASI_CONTROL_TYPE.ASI_GAIN:
							m_bGainAutoChecked = true;
							break;
						case ASICameraDll2.ASI_CONTROL_TYPE.ASI_EXPOSURE:
							m_bExposureAutoChecked = true;
							break;
						case ASICameraDll2.ASI_CONTROL_TYPE.ASI_WB_R:
						case ASICameraDll2.ASI_CONTROL_TYPE.ASI_WB_B:
							m_bWhiteBalanceAutoChecked = true;
							break;
						case ASICameraDll2.ASI_CONTROL_TYPE.ASI_BANDWIDTHOVERLOAD:
							m_bBandWidthAutoChecked = true;
							break;
						default:
							break;
					}
				}
				else
				{
					switch (type)
					{
						case ASICameraDll2.ASI_CONTROL_TYPE.ASI_GAIN:
							m_bGainAutoChecked = false;
							break;
						case ASICameraDll2.ASI_CONTROL_TYPE.ASI_EXPOSURE:
							m_bExposureAutoChecked = false;
							break;
						case ASICameraDll2.ASI_CONTROL_TYPE.ASI_WB_R:
						case ASICameraDll2.ASI_CONTROL_TYPE.ASI_WB_B:
							m_bWhiteBalanceAutoChecked = false;
							break;
						case ASICameraDll2.ASI_CONTROL_TYPE.ASI_BANDWIDTHOVERLOAD:
							m_bBandWidthAutoChecked = false;
							break;
						default:
							break;
					}
				}
				return true;
			}
			else
			{
				PopupMessageBox("Set Control Value Auto Fail: " + err.ToString());
				return false;
			}
		}

		public bool getControlValue(ASICameraDll2.ASI_CONTROL_TYPE type, out int iValue)
		{			
			ASICameraDll2.ASI_ERROR_CODE err = ASICameraDll2.ASIGetControlValue(m_iCameraID, type, out iValue);
			if(err == ASICameraDll2.ASI_ERROR_CODE.ASI_SUCCESS)
            {
				return true;
            }
			else
            {
				//PopupMessageBox("Get Control Value Fail: " + err.ToString());
				return false;
			}
		}
		public string scan()
        {
			int cameraNum = ASICameraDll2.ASIGetNumOfConnectedCameras();
			// Consider only one camera connection
			if (cameraNum > 0)
			{
				ASICameraDll2.ASI_CAMERA_INFO camInfoTemp;
				ASICameraDll2.ASIGetCameraProperty(out camInfoTemp, 0);
				m_cameraName = camInfoTemp.Name;

				m_iCameraID = camInfoTemp.CameraID;
				m_cameraName = camInfoTemp.Name;
				m_iMaxWidth = camInfoTemp.MaxWidth;
				m_iMaxHeight = camInfoTemp.MaxHeight;
				m_bIsColor = camInfoTemp.IsColorCam == ASICameraDll2.ASI_BOOL.ASI_TRUE ? true : false;
				m_bIsCooler = camInfoTemp.IsCoolerCam == ASICameraDll2.ASI_BOOL.ASI_TRUE ? true : false;
				m_bIsUSB3 = camInfoTemp.IsUSB3Camera == ASICameraDll2.ASI_BOOL.ASI_TRUE ? true : false;
				m_bIsUSB3Host = camInfoTemp.IsUSB3Host == ASICameraDll2.ASI_BOOL.ASI_TRUE ? true : false;				
			}
			else
            {
				m_cameraName = "";
			}
			return m_cameraName;
		}
		public bool setImageFormat(int width, int height, int startx, int starty, int bin, ASICameraDll2.ASI_IMG_TYPE type)
		{
			bool bCanStartThread = false;
			if(!m_bThreadStop && m_bThreadRunning)
            {
				stopCapture();
				bCanStartThread = true;
			}
			
			ASICameraDll2.ASI_ERROR_CODE err = ASICameraDll2.ASISetROIFormat(m_iCameraID, width, height, bin, type);
			if (err != ASICameraDll2.ASI_ERROR_CODE.ASI_SUCCESS)
            {
				/*
					int iWidth,  the width of the ROI area. Make sure iWidth%8 == 0. 
					int iHeight,  the height of the ROI area. Make sure iHeight%2 == 0, 
					further, for USB2.0 camera ASI120, please make sure that iWidth*iHeight%1024=0. 
				*/
				//if(iWidth%8 !=0 || iHeight%2 != 0)
				//{
				//    MessageBox.Show("Wrong Resolution");
				//    return;
				//}

				PopupMessageBox("SetFormat Error: " + err.ToString());
				return false;
            }

            m_iCurWidth = width;
            m_iCurHeight = height;
            m_iSize = m_iMaxWidth * m_iMaxHeight;
			m_iBin = bin;
			m_imgType = type;

			ASICameraDll2.ASISetStartPos(m_iCameraID, startx, starty);
			ASICameraDll2.ASIGetStartPos(m_iCameraID, out startx, out starty);

			if (bCanStartThread)
            {
				startCapture();
            }

			return true;
		}

		#region RefreshUI delegate
		// RefreshUI delegate
		public delegate void RefreshUICallBack(Bitmap bmp);
		private RefreshUICallBack RefreshUI;
		private bool m_bThreadRunning = false;
		private bool m_bThreadStop = false;
		private bool m_bThreadExit = false;
		IntPtr buffer = IntPtr.Zero;
		public void SetRefreshUICallBack(RefreshUICallBack callBack)
		{
			RefreshUI = callBack;
		}
		// MessageBox delegate
		public delegate void MessageBoxCallBack(string str, int iVal = 0);
		private MessageBoxCallBack PopupMessageBox;
		public void SetMessageBoxCallBack(MessageBoxCallBack callBack)
		{
			PopupMessageBox = callBack;
		}
		#endregion

		#region Capture thread
		// Capture thread
		public void startCaptureThread()
		{
			if (!m_bThreadRunning)
			{
				m_bThreadStop = false;
				captureThread.Start();
			}
			else
			{
				m_bThreadStop = false;
			}
		}
		public void stopCaptureThread()
		{
			m_bThreadStop = true;
		}
		public void exitCaptureThread()
		{
			m_bThreadExit = true;
		}
		public void run()
		{
			m_bThreadRunning = true;

			while (true)
			{
				if (m_bThreadExit)
				{
					break;
				}
				if(m_bThreadStop)
                {
					continue;
				}

				int cameraID = m_iCameraID;
				int width = m_iCurWidth;
				int height = m_iCurHeight;
				int buffersize = 0;
				if (m_imgType == ASICameraDll2.ASI_IMG_TYPE.ASI_IMG_RAW8 || m_imgType == ASICameraDll2.ASI_IMG_TYPE.ASI_IMG_Y8)
					buffersize = width * height;
				if (m_imgType == ASICameraDll2.ASI_IMG_TYPE.ASI_IMG_RAW16)
					buffersize = width * height * 2;
				if (m_imgType == ASICameraDll2.ASI_IMG_TYPE.ASI_IMG_RGB24)
					buffersize = width * height * 3;

				buffer = Marshal.AllocCoTaskMem(buffersize);

				if (m_CaptureMode == CaptureMode.Video)
				{
					int expMs;
					ASICameraDll2.ASIGetControlValue(cameraID, ASICameraDll2.ASI_CONTROL_TYPE.ASI_EXPOSURE, out expMs);
					expMs /= 1000;

					ASICameraDll2.ASI_ERROR_CODE err = ASICameraDll2.ASIGetVideoData(cameraID, buffer, buffersize, expMs * 2 + 500);
                    if (err == ASICameraDll2.ASI_ERROR_CODE.ASI_SUCCESS)
                    {
                        byte[] byteArray = new byte[buffersize];
                        Marshal.Copy(buffer, byteArray, 0, buffersize);
                        Marshal.FreeCoTaskMem(buffer);
                        Bitmap bmp = new Bitmap(width, height);
						int index = 0;

						var lockBitmap = new LockBitmap(bmp);
						lockBitmap.LockBits();                        
                        for (int i = 0; i < height; i++)
                        {
                            for (int j = 0; j < width; j++)
                            {
                                if (m_bThreadStop)
                                {
                                    goto NEXT_LOOP;
                                }

                                if (m_imgType == ASICameraDll2.ASI_IMG_TYPE.ASI_IMG_RAW8 || m_imgType == ASICameraDll2.ASI_IMG_TYPE.ASI_IMG_Y8)
                                {
									lockBitmap.SetPixel(j, i, Color.FromArgb(byteArray[index], byteArray[index], byteArray[index]));
								}
                                else if (m_imgType == ASICameraDll2.ASI_IMG_TYPE.ASI_IMG_RAW16)
                                {
									lockBitmap.SetPixel(j, i, Color.FromArgb(byteArray[index * 2 + 1], byteArray[index * 2 + 1], byteArray[index * 2 + 1]));
								}
								else if (m_imgType == ASICameraDll2.ASI_IMG_TYPE.ASI_IMG_RGB24)
                                {
									lockBitmap.SetPixel(j, i, Color.FromArgb(byteArray[index * 3 + 0], byteArray[index * 3 + 1], byteArray[index * 3 + 2]));
								}

								index++;
                            }
                        }
						lockBitmap.UnlockBits();

						RefreshUI(bmp);
                    }
                    else
                    {
                        Marshal.FreeCoTaskMem(buffer);
                    }
                }
				else if(m_CaptureMode == CaptureMode.Snap)
                {
					ASICameraDll2.ASI_ERROR_CODE err;
					err = ASICameraDll2.ASIStartExposure(cameraID, ASICameraDll2.ASI_BOOL.ASI_FALSE);

					ASICameraDll2.ASI_EXPOSURE_STATUS status;
					do
					{
						ASICameraDll2.ASIGetExpStatus(cameraID, out status);
					}
					while (status == ASICameraDll2.ASI_EXPOSURE_STATUS.ASI_EXP_WORKING);

					if (status != ASICameraDll2.ASI_EXPOSURE_STATUS.ASI_EXP_SUCCESS)
					{
						Marshal.FreeCoTaskMem(buffer);
						continue;
					}
					if (ASICameraDll2.ASIGetDataAfterExp(cameraID, buffer, buffersize) == ASICameraDll2.ASI_ERROR_CODE.ASI_SUCCESS)
					{
						byte[] byteArray = new byte[buffersize];
						Marshal.Copy(buffer, byteArray, 0, buffersize);
						Marshal.FreeCoTaskMem(buffer);
						Bitmap bmp = new Bitmap(width, height);
						int index = 0;

						var lockBitmap = new LockBitmap(bmp);
						lockBitmap.LockBits();
						for (int i = 0; i < height; i++)
						{
							for (int j = 0; j < width; j++)
							{
								if (m_imgType == ASICameraDll2.ASI_IMG_TYPE.ASI_IMG_RAW8 || m_imgType == ASICameraDll2.ASI_IMG_TYPE.ASI_IMG_Y8)
								{
									lockBitmap.SetPixel(j, i, Color.FromArgb(byteArray[index], byteArray[index], byteArray[index]));
								}
								else if (m_imgType == ASICameraDll2.ASI_IMG_TYPE.ASI_IMG_RAW16)
								{
									lockBitmap.SetPixel(j, i, Color.FromArgb(byteArray[index * 2 + 1], byteArray[index * 2 + 1], byteArray[index * 2 + 1]));
								}
								else if (m_imgType == ASICameraDll2.ASI_IMG_TYPE.ASI_IMG_RGB24)
								{
									lockBitmap.SetPixel(j, i, Color.FromArgb(byteArray[index * 3 + 0], byteArray[index * 3 + 1], byteArray[index * 3 + 2]));
								}
								index++;
							}
						}
						lockBitmap.UnlockBits();
						RefreshUI(bmp);
					}
					else
                    {
						Marshal.FreeCoTaskMem(buffer);
					}
					stopCaptureThread();
				}
				NEXT_LOOP: ;
                
            }
		}




		public class LockBitmap
		{
			private readonly Bitmap _source = null;
			IntPtr _iptr = IntPtr.Zero;
			BitmapData _bitmapData = null;

			public byte[] Pixels { get; set; }
			public int Depth { get; private set; }
			public int Width { get; private set; }
			public int Height { get; private set; }

			public LockBitmap(Bitmap source)
			{
				this._source = source;
			}
			public Bitmap getBitmap()
			{
				return _source;
			}
			/// <summary>
			/// 锁定位图数据
			/// </summary>
			public void LockBits()
			{
				try
				{
					// 获取位图的宽和高
					Width = _source.Width;
					Height = _source.Height;

					// 获取锁定像素点的总数
					int pixelCount = Width * Height;

					// 创建锁定的范围
					Rectangle rect = new Rectangle(0, 0, Width, Height);

					// 获取像素格式大小
					Depth = Image.GetPixelFormatSize(_source.PixelFormat);

					// 检查像素格式
					if (Depth != 8 && Depth != 24 && Depth != 32)
					{
						throw new ArgumentException("仅支持8,24和32像素位数的图像");
					}

					// 锁定位图并返回位图数据
					_bitmapData = _source.LockBits(rect, ImageLockMode.ReadWrite, _source.PixelFormat);

					// 创建字节数组以复制像素值
					int step = Depth / 8;
					Pixels = new byte[pixelCount * step];
					_iptr = _bitmapData.Scan0;

					// 将数据从指针复制到数组
					Marshal.Copy(_iptr, Pixels, 0, Pixels.Length);
				}
				catch (Exception ex)
				{
					throw ex;
				}
			}

			/// <summary>
			/// 解锁位图数据
			/// </summary>
			public void UnlockBits()
			{
				try
				{
					// 将数据从字节数组复制到指针
					Marshal.Copy(Pixels, 0, _iptr, Pixels.Length);

					// 解锁位图数据
					_source.UnlockBits(_bitmapData);
				}
				catch (Exception ex)
				{
					throw ex;
				}
			}

			/// <summary>
			/// 获取像素点的颜色
			/// </summary>
			/// <param name="x"></param>
			/// <param name="y"></param>
			/// <returns></returns>
			public Color GetPixel(int x, int y)
			{
				Color clr = Color.Empty;

				// 获取颜色组成数量
				int cCount = Depth / 8;

				// 获取指定像素的起始索引
				int i = ((y * Width) + x) * cCount;

				if (i > Pixels.Length - cCount)
					throw new IndexOutOfRangeException();

				if (Depth == 32) // 获得32 bpp红色，绿色，蓝色和Alpha
				{
					byte b = Pixels[i];
					byte g = Pixels[i + 1];
					byte r = Pixels[i + 2];
					byte a = Pixels[i + 3]; // a
					clr = Color.FromArgb(a, r, g, b);
				}

				if (Depth == 24) // 获得24 bpp红色，绿色和蓝色
				{
					byte b = Pixels[i];
					byte g = Pixels[i + 1];
					byte r = Pixels[i + 2];
					clr = Color.FromArgb(r, g, b);
				}

				if (Depth == 8) // 获得8 bpp
				{
					byte c = Pixels[i];
					clr = Color.FromArgb(c, c, c);
				}
				return clr;
			}

			/// <summary>
			/// 设置像素点颜色
			/// </summary>
			/// <param name="x"></param>
			/// <param name="y"></param>
			/// <param name="color"></param>
			public void SetPixel(int x, int y, Color color)
			{
				// 获取颜色组成数量
				int cCount = Depth / 8;

				// 获取指定像素的起始索引
				int i = ((y * Width) + x) * cCount;

				if (Depth == 32)
				{
					Pixels[i] = color.B;
					Pixels[i + 1] = color.G;
					Pixels[i + 2] = color.R;
					Pixels[i + 3] = color.A;
				}
				if (Depth == 24)
				{
					Pixels[i] = color.B;
					Pixels[i + 1] = color.G;
					Pixels[i + 2] = color.R;
				}
				if (Depth == 8)
				{
					Pixels[i] = color.B;
				}
			}
		}

		#endregion
	}
}
