using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gabriel.Cat.Media
{
    public class FFMPEGHelper
    {
        public enum Format
        {
            mp4,
            mov,
            flv,
            wmv
            //añadir todos los compatibles
        }
        public enum VideoCodec
        {
            H264
        }
        public enum AudioCodec
        {
            AAC
        }
        public enum AudioOption
        {
            Mono = 1,
            Stereo
        }
        public enum AudioBitrate
        {
            K128 = 128000,
        }
        public enum TranscodeSpeed
        {
            UltraFast,
            SuperFast,
            VertFast,
            Faster,
            Fast,
            Medium,
            Slow,
            Slower,
            VerySlow
        }
        public class Config
        {
            TranscodeSpeed? doitSpeed;
            Format? outputFormat;
            TimeSpan? initialTime;
            TimeSpan? duration;
            VideoCodec? videoCodec;
            AudioCodec? audioCodec;
            AudioOption? audioOption;
            AudioBitrate? audioBitrate;
            int? height;
            int? width;

            public Config(TimeSpan? initialTime = null, TimeSpan? duration = null, VideoCodec? videoCodec = null, AudioCodec? audioCodec = null, AudioOption? audioOption = null, AudioBitrate? audioBitrate = null, int? height = null, int? width = null, TranscodeSpeed? doitSpeed = null, Format? outputFormat = Format.mp4)
            {

                this.initialTime = initialTime;
                this.duration = duration;
                this.videoCodec = videoCodec;
                this.audioCodec = audioCodec;
                this.audioOption = audioOption;
                this.audioBitrate = audioBitrate;
                this.height = height;
                this.width = width;
                this.doitSpeed = doitSpeed;
                this.outputFormat = outputFormat;
            }

            public TimeSpan? InitialTime { get => initialTime; set => initialTime = value; }
            public VideoCodec? VideoCodec { get => videoCodec; set => videoCodec = value; }
            public AudioCodec? AudioCodec { get => audioCodec; set => audioCodec = value; }
            public AudioOption? AudioOption { get => audioOption; set => audioOption = value; }
            public AudioBitrate? AudioBitrate { get => audioBitrate; set => audioBitrate = value; }
            public int? Height { get => height; set => height = value; }
            public int? Width { get => width; set => width = value; }
            public TimeSpan? Duration { get => duration; set => duration = value; }
            public TranscodeSpeed? DoItSpeed { get => doitSpeed; set => doitSpeed = value; }
            public Format? OutputFormat { get => outputFormat; set => outputFormat = value; }

            public string GetString(FileInfo file, string outputPath = null)
            {
                return GetString(file.FullName, outputPath);
            }
            public string GetString(string filePath, string outputPath = null)
            {
                StringBuilder str = new StringBuilder(PROGRAM);
                if (InitialTime.HasValue)
                    SetParam(str, "-ss", InitialTime.ToString());
                SetParam(str, TRANSCODE, "\"" + filePath + "\"");
                if (VideoCodec.HasValue)
                {
                    switch (VideoCodec)
                    {
                        case FFMPEGHelper.VideoCodec.H264:
                            SetParam(str, "-c:v", "libx264");
                            break;
                    }
                }
                if (AudioCodec.HasValue)
                {
                    switch (AudioCodec)
                    {
                        case FFMPEGHelper.AudioCodec.AAC:
                            SetParam(str, "-c:a", "libvo_aacenc");
                            break;
                    }
                }
                if (AudioOption.HasValue)
                {
                    SetParam(str, "-ac", (int)AudioOption + "");
                }
                if (AudioBitrate.HasValue)
                {
                    SetParam(str, "-ab", (int)AudioBitrate + "");
                }
                if (Height.HasValue && Width.HasValue)
                {
                    SetParam(str, "-s", string.Format("{0}x{1}", Height, Width));
                }
                if (Duration.HasValue)
                {
                    SetParam(str, "-t", Duration.ToString());
                }
                if (DoItSpeed.HasValue)
                {
                    SetParam(str, "-preset", DoItSpeed.ToString().ToLower());
                }

                if (OutputFormat.HasValue)
                {
                    if (outputPath == null)
                        outputPath = Path.Combine(Environment.CurrentDirectory, Path.GetFileNameWithoutExtension(filePath), ".", OutputFormat.ToString());
                    else if (outputPath[outputPath.Length - 1] == '_' || !outputPath.Contains('.'))
                        outputPath += "." + OutputFormat.ToString();
                }
                else if (outputPath[outputPath.Length - 1] == '_' || !outputPath.Contains('.'))
                    outputPath += "." + filePath.Split('.').Last();

                SetParam(str, "\"" + outputPath + "\"");

                return str.ToString();
            }

        }
        struct SnapShotProcess
        {
            System.Diagnostics.Process process;
            FileInfo file;

            public SnapShotProcess(Process process, FileInfo file)
            {
                this.process = process;
                this.file = file;
            }

            public Process Process { get => process; }
            public FileInfo File { get => file; }
        }
        public const string PROGRAM = "ffmpeg";
        public const string TRANSCODE = "-i";

        public static string FFMPEGPath = "ffmpeg.exe";

        public FileInfo File { get; set; }
        public DirectoryInfo OutputDir { get; set; }
        public string OutputName { get; set; }
        public Config Configuration { get; set; }

        public FileInfo SnapShot(TimeSpan time, string output = null, int frames = 1)
        {
            return ISnapShot(time, output, frames).File;
        }
        SnapShotProcess ISnapShot(TimeSpan time, string output = null, int frames = 1)
        {
            string stringSnapShot = GetStringTakeSnapShot(File, time, output, frames);
            string outputPath = GetOutPath(stringSnapShot);
            System.Diagnostics.Process process = Run(stringSnapShot);
            process.WaitForExit();
            return new SnapShotProcess(process, new FileInfo(outputPath));
        }
        public Bitmap GetSnapShot(TimeSpan time)
        {

            SnapShotProcess fileAndProcess = ISnapShot(time);
            Bitmap bmp = null;
            Task asyncDelete;
            bmp = new Bitmap(fileAndProcess.File.FullName);
            //al parecer después de hacer el snapshot el ffmpeg continua trabajando...y no puedo hacerlo facil...de momento...
            //if (!fileAndProcess.Process.HasExited)
            //    fileAndProcess.Process.Kill();
            //fileAndProcess.File.Delete();
            asyncDelete = new Task(() =>
              {
                  const int MAXINTENTS = 10 * 60;//un minuto
                  int intents = 0;
                  while (System.IO.File.Exists(fileAndProcess.File.FullName) && intents++ < MAXINTENTS)
                      try
                      {
                          fileAndProcess.File.Delete();
                      }
                      catch { System.Threading.Thread.Sleep(100); }
              });
            asyncDelete.Start();


            return bmp;
        }
        public FileInfo Transcode()
        {
            string stringTranscode = ToString();
            string outputPath = GetOutPath(stringTranscode);
            Run(stringTranscode).WaitForExit();
            return new FileInfo(outputPath);
        }

        private string GetOutPath(string stringCmd)
        {
            string[] parts = stringCmd.Split('"');
            return parts[parts.Length - 2];
        }

        public override string ToString()
        {
            string pathOutput = Path.Combine(OutputDir != null ? OutputDir.FullName : Environment.CurrentDirectory, String.IsNullOrEmpty(OutputName) ? File.Name + "_" : OutputName);
            return GetTranscodeString(File, pathOutput, Configuration);
        }

        public static System.Diagnostics.Process Run(string commandToRun)
        {
            if (commandToRun.Contains(PROGRAM))
                commandToRun = commandToRun.Remove(0, PROGRAM.Length + 1);
            System.Diagnostics.Process process;
            System.Diagnostics.ProcessStartInfo processStartInfo = new System.Diagnostics.ProcessStartInfo();
            processStartInfo.FileName = FFMPEGPath;
            processStartInfo.Arguments = commandToRun;
            processStartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            process = new System.Diagnostics.Process() { StartInfo = processStartInfo };
            process.Start();
            return process;
        }

        public static string GetStringTakeSnapShot(FileInfo file, TimeSpan time, string output = null, int frames = 1)
        {
            StringBuilder str = new StringBuilder(PROGRAM);
            SetParam(str, "-ss", time.ToString(), TRANSCODE, "\"" + file.FullName + "\"",  "-vframes", frames + "", output == null ? "\"" + file.Name + "_" + DateTime.Now.Ticks + ".jpg" + "\"" : "\"" + output + "\"");
            return str.ToString();
        }
        public static string GetTranscodeString(FileInfo file, string outputPath = null, Config config = null)
        {//si no cambio de formato se cambia la resolucion....
            if (file == null || !file.Exists)
                throw new FileNotFoundException();
            StringBuilder str;


            if (config == null)
            {
                str = new StringBuilder(PROGRAM);
                str.Append(' ');
                str.Append(TRANSCODE);
                str.Append(' ');
                str.Append("\"" + file.FullName + "\"");
                str.Append(' ');
                str.Append("\"" + outputPath + "\"");
            }
            else str = new StringBuilder(config.GetString(file, outputPath));

            return str.ToString();
        }

        static void SetParam(StringBuilder str, params string[] paramsToSet)
        {
            for (int i = 0; i < paramsToSet.Length; i++)
            {
                str.Append(' ');
                str.Append(paramsToSet[i]);
            }
        }
    }
}
