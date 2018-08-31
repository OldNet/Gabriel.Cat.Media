using System;
using System.Collections.Generic;
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
                SetParam(str, TRANSCODE, filePath);
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
                    else if (!outputPath.Contains('.'))
                        outputPath += "." + OutputFormat.ToString();
                }else if (!outputPath.Contains('.'))
                    outputPath +="."+filePath.Split('.').Last();

                SetParam(str, outputPath);

                return str.ToString();
            }
          
        }

        public const string PROGRAM = "ffmpeg";
        public const string TRANSCODE = "-i";

        public static string FFMPEGPath = "ffmpeg.exe";

        public FileInfo File { get; set; }
        public DirectoryInfo OutputDir { get; set; }
        public string OutputName { get; set; }
        public Config Configuration { get; set; }

        public FileInfo SnapShot(TimeSpan time,string output=null,int frames=1)
        {
            string stringSnapShot = GetStringTakeSnapShot(File, time, output, frames);
            string outputPath=stringSnapShot.Split(' ').Last();
            Run(stringSnapShot);
            return new FileInfo(outputPath);
        }
        public FileInfo Transcode()
        {
            string stringTranscode = ToString();
            string outputPath = stringTranscode.Split(' ').Last();
            Run(stringTranscode);
            return new FileInfo(outputPath);
        }       
        public override string ToString()
        {
            string pathOutput = Path.Combine(OutputDir!=null?OutputDir.FullName:Environment.CurrentDirectory, String.IsNullOrEmpty(OutputName) ? File.Name : OutputName);
            return GetTranscodeString(File, pathOutput, Configuration);
        }

        public static void Run(string commandToRun)
        {
            if (commandToRun.Contains(PROGRAM))
                commandToRun = commandToRun.Remove(0, PROGRAM.Length + 1);
            System.Diagnostics.ProcessStartInfo processStartInfo = new System.Diagnostics.ProcessStartInfo();
            processStartInfo.FileName = FFMPEGPath;
            processStartInfo.Arguments = commandToRun;
            new System.Diagnostics.Process() { StartInfo = processStartInfo }.Start();
        }
       
        public static string GetStringTakeSnapShot(FileInfo file,TimeSpan time, string output = null, int frames = 1)
        {
            StringBuilder str = new StringBuilder(PROGRAM);
            SetParam(str, TRANSCODE, file.FullName, "-ss", time.ToString(), "-vframes", frames + "", output == null ? file.Name + "_" + DateTime.Now.Ticks + ".jpg" : output);
            return str.ToString();
        }
        public static string GetTranscodeString(FileInfo file, string outputPath = null, Config config = null)
        {
            if (file == null || !file.Exists)
                throw new FileNotFoundException();
            StringBuilder str;


            if (config == null)
            {
                str = new StringBuilder(PROGRAM);
                str.Append(' ');
                str.Append(TRANSCODE);
                str.Append(' ');
                str.Append(file.FullName);
                str.Append(' ');
                str.Append(outputPath);
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
