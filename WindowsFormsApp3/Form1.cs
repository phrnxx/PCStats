using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Management;
using OpenHardwareMonitor.Hardware;
using System.Diagnostics;
using System.ServiceProcess;

namespace PCStatsApp
{
    public partial class MainForm : Form
    {
       
        private readonly Computer computer;
        private Thread updateThread;
        private bool isFormClosing = false;
        private string processorName = "";
        private string gpuName = "";
        private string motherboardName = "";
       

        public MainForm()
        {
            InitializeComponent();
           

            computer = new Computer();
            computer.Open();

            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.BackColor = Color.Black;
            this.ForeColor = Color.White;
            this.MaximumSize = this.Size;
            this.MinimumSize = this.Size;

            computer.CPUEnabled = true;
            computer.GPUEnabled = true;
            computer.MainboardEnabled = true;

            updateThread = new Thread(UpdateSystemInfo);
            updateThread.IsBackground = true;
            updateThread.Start();

            groupBox1.Text = "Процессор";
            groupBox2.Text = "Видеокарта";
            groupBox3.Text = "Память";


            this.FormClosing += MainForm_FormClosing;
        }

       
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            isFormClosing = true;
            if (updateThread != null && updateThread.IsAlive)
            {
                updateThread.Interrupt();
                if (!updateThread.Join(600)) // Обновление каждие 6с 
                {
                    updateThread.Abort(); // Принудительное завершение потока
                }
            }
            computer.Close();
        }

        private void UpdateSystemInfo()
        {
            try
            {
                while (!isFormClosing)
                {
                    UpdateProcessorInfo();
                    UpdateGPUInfo();
                    UpdateMemoryInfo();
                    Thread.Sleep(1000);
                }
            }
            catch (ThreadInterruptedException)
            {
                // Поток был прерван, выйти из метода
            }
        }


        //Processor
        private void UpdateProcessorInfo()
        {
            foreach (var hardware in computer.Hardware)
            {
                if (hardware.HardwareType == HardwareType.CPU)
                {
                    hardware.Update();
                    processorName = hardware.Name;
                    string processorBrand = "";
                    string processorModel = "";
                    int numberOfCores = 0;
                    float processorLoad = 0;
                    float processorTemperature = 0;
                    int loadSensorsCount = 0;

                    if (processorName.Contains("Intel"))
                    {
                        processorBrand = "Intel";
                        processorModel = processorName.Replace("Intel ", "").Trim();
                    }
                    else if (processorName.Contains("AMD"))
                    {
                        processorBrand = "AMD";
                        int startIndex = processorName.IndexOf("AMD ") + 4;
                        processorModel = processorName.Substring(startIndex);
                    }

                    foreach (var sensor in hardware.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Temperature)
                        {
                            if (sensor.Name.ToLower().Contains("core"))
                            {
                                processorTemperature += sensor.Value.GetValueOrDefault();
                                numberOfCores++;
                            }
                            else if (processorBrand == "AMD" && sensor.Name.ToLower().Contains("package"))
                            {
                                processorTemperature += sensor.Value.GetValueOrDefault();
                            }
                        }
                        else if (sensor.SensorType == SensorType.Load && sensor.Name.ToLower().Contains("total"))
                        {
                            processorLoad += sensor.Value.GetValueOrDefault();
                            loadSensorsCount++;
                        }
                    }

                    if (numberOfCores > 0)
                        processorTemperature /= numberOfCores;
                    else if (processorBrand == "AMD")
                        processorTemperature /= 1; 

                    if (loadSensorsCount > 0)
                        processorLoad /= loadSensorsCount;

                    int roundedTemperature = (int)Math.Round(processorTemperature, 0);
                    int roundedLoad = (int)Math.Round(processorLoad, 0);

                    if (this.InvokeRequired)
                    {
                        this.BeginInvoke((MethodInvoker)delegate
                        {
                            UpdateProcessorInfoUI(processorName, processorBrand, processorModel, numberOfCores, roundedLoad, roundedTemperature);
                        });
                    }
                    else
                    {
                        UpdateProcessorInfoUI(processorName, processorBrand, processorModel, numberOfCores, roundedLoad, roundedTemperature);
                    }

                    break;
                }
            }
        }

        private void UpdateProcessorInfoUI(string processorName, string processorBrand, string processorModel, int numberOfCores, int roundedLoad, int roundedTemperature)
        {
            label14.Text = $"{processorBrand} {processorModel}";
            label1.Text = $"Количество ядер: {numberOfCores}";
            label2.Text = $"Загрузка процессора: {roundedLoad}%";
            label3.Text = $"Температура процессора: {roundedTemperature}°C";
        }

        //GPU
        private void UpdateGPUInfo()
        {
            foreach (var hardware in computer.Hardware)
            {
                if (hardware.HardwareType == HardwareType.GpuNvidia)
                {
                    hardware.Update();

                    gpuName = hardware.Name;
                    float gpuTemperature = 0;
                    float gpuLoad = 0;
                    float gpuMemoryUsage = 0;
                    float gpuVideoEngineLoad = 0;
                    float gpuBusInterfaceLoad = 0;

                    foreach (var sensor in hardware.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Temperature)
                        {
                            gpuTemperature = sensor.Value.GetValueOrDefault();
                        }
                        else if (sensor.SensorType == SensorType.Load && sensor.Name.ToLower().Contains("gpu core"))
                        {
                            gpuLoad = sensor.Value.GetValueOrDefault();
                        }
                        else if (sensor.SensorType == SensorType.Load && sensor.Name.ToLower().Contains("memory controller"))
                        {
                            gpuMemoryUsage = sensor.Value.GetValueOrDefault();
                        }
                        else if (sensor.SensorType == SensorType.Load && sensor.Name.ToLower().Contains("video engine"))
                        {
                            gpuVideoEngineLoad = sensor.Value.GetValueOrDefault();
                        }
                        else if (sensor.SensorType == SensorType.Load && sensor.Name.ToLower().Contains("bus interface"))
                        {
                            gpuBusInterfaceLoad = sensor.Value.GetValueOrDefault();
                        }
                    }

                    if (this.InvokeRequired)
                    {
                        this.BeginInvoke((MethodInvoker)delegate
                        {
                            UpdateGPUInfoUI(gpuName, gpuTemperature, gpuLoad, gpuMemoryUsage, gpuVideoEngineLoad, gpuBusInterfaceLoad);
                        });
                    }
                    else
                    {
                        UpdateGPUInfoUI(gpuName, gpuTemperature, gpuLoad, gpuMemoryUsage, gpuVideoEngineLoad, gpuBusInterfaceLoad);
                    }

                    break;
                }
            }
        }

        private void UpdateGPUInfoUI(string gpuName, float gpuTemperature, float gpuLoad, float gpuMemoryUsage, float gpuVideoEngineLoad, float gpuBusInterfaceLoad)
        {
            groupBox2.Text = $"Видеокарта: {gpuName}";
            label6.Text = $"Температура GPU: {gpuTemperature}°C";
            label7.Text = $"Загрузка GPU: {gpuLoad}%";
            label10.Text = $"Загрузка шины GPU: {gpuBusInterfaceLoad}%";
        }

        //RAM
        private void UpdateMemoryInfo()
        {
            ObjectQuery queryRam = new ObjectQuery("SELECT * FROM Win32_OperatingSystem");
            ManagementObjectSearcher searcherRam = new ManagementObjectSearcher(queryRam);
            foreach (ManagementObject obj in searcherRam.Get())
            {
                double totalMemoryGB = Math.Round(Convert.ToDouble(obj["TotalVisibleMemorySize"]) / 1024.0 / 1024.0, 2);
                double freeMemoryGB = Math.Round(Convert.ToDouble(obj["FreePhysicalMemory"]) / 1024.0 / 1024.0, 2);
                double memoryLoad = 100 - Math.Round((freeMemoryGB / totalMemoryGB) * 100);

                this.Invoke((MethodInvoker)delegate
                {
                    label11.Text = $"Общая память: {totalMemoryGB} GB";
                    label12.Text = $"Свободная память: {freeMemoryGB} GB";
                    label13.Text = $"Загрузка памяти: {memoryLoad}%";
                });

                break;
            }
        }
    }
}