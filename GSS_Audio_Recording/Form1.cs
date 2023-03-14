
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Net.Sockets;
using NAudio.Wave;
using System.Media;
using NAudio;
using System.Net;
using System.Windows.Forms;
using System.Timers;

namespace GSS_Audio_Recording
{
    public partial class Form1 : Form
    {

        string record;//Used as a global to determine if the voice recording is still hapening in secret.
        bool recording = false;//Unused but kept if we want to globally track whether it is recording or not
        private static System.Timers.Timer recordingTimeoutTimer;

        public Form1()
        {

            this.WindowState = FormWindowState.Minimized;//Hide from surveyor
            this.ShowInTaskbar = false; // This is optional
            waveSource = new WaveInEvent();//Needs to be initialised for event firing in recording function.
            InitializeComponent();
            closeFirstInstance();//Close any existing version that is all ready running
            record = "record";//arbritrary and to use if more flags are introduced in future
            Directory.CreateDirectory(@"C:\RecordedQuestionsGSS_FTP\");//Directory where recorded questions will end up
            RecordInSecret();


        }

        //Close already existing instance of this application - can only have one running at a time.
        private void closeFirstInstance()
        {
            Process[] pname = Process.GetProcessesByName(AppDomain.CurrentDomain.FriendlyName.Remove(AppDomain.CurrentDomain.FriendlyName.Length - 4));
            if (pname.Length > 1)
            {
                pname[1].Kill();
            }
        }

        static WaveFileWriter waveFile;
        static WaveInEvent waveSource;
        private void RecordInSecret()
        {


            string fileName = null;
            string userName = System.Environment.UserName;

            if (record == "record")//left in case we need any other flags to permit recording
            {
                string info = "_TemporaryFileName";//Sets file name to temp to be changed later by parameters given by sample manager

                //Initialise the .wav file we intend to record
                waveSource = new WaveInEvent();
                waveSource.WaveFormat = new WaveFormat(5000, 1);
                waveSource.DataAvailable += new EventHandler<WaveInEventArgs>(waveSource_DataAvailable);

                //Timestamp format like all other audio recording CBG applications
                fileName = DateTime.Now.ToString("yyyy/MM/dd") + "T" + DateTime.Now.ToString("HH:mm:ss");
                fileName = fileName.Replace("/", "");
                fileName = fileName.Replace(":", "");
                fileName = fileName + info;


                string tempFile = (@"C:\RecordedQuestionsNZSAS_FTP\" + fileName + ".wav");//location that the recorded files are stored
                waveFile = new WaveFileWriter(tempFile, waveSource.WaveFormat);
                waveSource.StartRecording();//Begin recording 
                SetTimer();//Initialise timer that waits for an hour time-out before ending recording
                recording = true;
            }

            while (true)
            {
                Thread.Sleep(100);
                if (CheckSurveyFinished() == true || timerElapsed == true)//Checks if the end recording flag has been set my sample manager, if if timer has timed out
                {
                    timerElapsed = false;
                    waveSource.StopRecording();
                    recording = false;
                    waveFile.Dispose();
                    RenameAudioFile(fileName);//rename audio file, removing the temp part and adding whatever is in C:\PIAAC\AudioRecording\SurveyIdentifiers.txt first line
                    break;
                }
            }

            //terminate program
            CloseRecordingProcess();

        }

        //This timer triggers an elapsed event after one hour, ensuring we can stop recording if recording period exceeds one hour
        private void SetTimer()
        {

            recordingTimeoutTimer = new System.Timers.Timer(3600000);//Set elapse period (1 hr atm)
            recordingTimeoutTimer.Elapsed += OnTimedEvent;
            recordingTimeoutTimer.AutoReset = false;
            recordingTimeoutTimer.Enabled = true;
        }

        //This event triggers on elapsed period. RecordInSecret() awaits timerElapsed == true to stop recording.
        bool timerElapsed = false;
        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            recording = false;
            timerElapsed = true;
            recordingTimeoutTimer.Enabled = false;
        }

        //Renames the audio file once recording is finished by accessing identifiers saved by Sample manager during the exit question process.
        private void RenameAudioFile(string fileName)
        {
            string identifiers = ReadFirstLine(@"C:\CBGShared\GSSRecording\SurveyIdentifiers.txt") + "_forupload";
            File.Move(@"C:\RecordedQuestionsNZSAS_FTP\" + fileName + ".wav", @"C:\RecordedQuestionsNZSAS_FTP\" + fileName.Replace("TemporaryFileName", identifiers) + ".wav");
        }

        //WriteData gets obsolete warnings but it works completely fine
        static void waveSource_DataAvailable(object sender, WaveInEventArgs e)
        {
            waveFile.Write(e.Buffer, 0, e.BytesRecorded);

        }


        private string ReadFirstLine(string textfile)
        {
            string permission = File.ReadLines(textfile).First();
            return permission;
        }

        //Hiding application from the surveyor
        private void Form1_Load(object sender, EventArgs e)
        {
            Opacity = 0;
            //RecordInSecret(); //Recording function upon load.
        }

        //Hiding application from the surveyor
        private void Form1_Shown(object sender, EventArgs e)
        {
            Visible = false;
            Opacity = 100;
        }

        //Obsolete kept for future reference
        private void CloseRecordingProcess()
        {
                foreach (Process proc in Process.GetProcessesByName("GSS_Audio_Recording"))
                {
                    proc.Kill();
                }
        }

        //Checks for "true" in C:\PIAAC\AudioRecording\ExitQuestionsOpened.txt and ensures it was set within last 10 seconds, allowing us to know that recording should be stopped.
        private bool CheckSurveyFinished()
        {
            DateTime SurveyFinishedQustionsWriteTime = File.GetLastWriteTime(@"C:\CBGshared\GSSRecording\SurveyFinished.txt");
            DateTime currentTime = DateTime.Now;
            string exitQuestionsTrueFalse = ReadFirstLine(@"C:\CBGshared\GSSRecording\SurveyFinished.txt");
            if (((currentTime - SurveyFinishedQustionsWriteTime).TotalSeconds < 10) && (exitQuestionsTrueFalse == "true"))
            {
                return true;
            }
            else
            {
                return false;
            }


        }

    }
}

