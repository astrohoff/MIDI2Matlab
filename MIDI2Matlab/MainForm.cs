using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Melanchall.DryWetMidi.Smf;

namespace MIDI2Matlab
{
    public partial class mainWindow : Form
    {
        MidiFile midiFile;

        public mainWindow()
        {
            InitializeComponent();
        }

        private void openButton_Click(object sender, EventArgs e)
        {
            if(openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                midiFile = MidiFile.Read(openFileDialog1.FileName);
                DisplayMidiInfo();
            }
        }

        void DisplayMidiInfo()
        {
            infoTextBox.Text = "";
            infoTextBox.Text += GetGlobalInfoString();
            // Change this to change # of tracks that are printed.
            int maxChunkPrintCount = 3;
            int chunkPrintCount = Math.Min(midiFile.Chunks.Count, maxChunkPrintCount);
            for (int i = 0; i < chunkPrintCount; i++)
            {
                infoTextBox.Text += GetChunkInfoString(i);
            }
        }

        string GetGlobalInfoString()
        {
            string str = "";
            str += "Global MIDI info:\r\n";
            str += GetTimeDivisionString();
            str += "Chunk count: " + midiFile.Chunks.Count + "\r\n";
            str = GetNullCleansed(str);
            return str; 
        }

        string GetChunkInfoString(int chunkIndex)
        {
            if (midiFile.Chunks[chunkIndex] is TrackChunk)
            {
                return GetFullTrackInfoString(chunkIndex);
            }
            else
            {
                return  "\tUnknown chunk type (chunk " + chunkIndex + ")\r\n";
            }
        }

        string GetFullTrackInfoString(int chunkIndex)
        {
            string str = "";
            str += "Chunk " + chunkIndex + " events:\r\n";
            TrackChunk track = (TrackChunk)midiFile.Chunks[chunkIndex];
            for(int i = 0; i < track.Events.Count; i++)
            {
                str += "\t" + track.Events[i].ToString() + "\r\n";
            }
            str = GetNullCleansed(str);
            return str;
        }

        string GetTimeDivisionString()
        {
            if(midiFile.TimeDivision is TicksPerQuarterNoteTimeDivision)
            {
                string str = "";
                str += "Time division format: ticks per quater note\r\n";
                int ticks = ((TicksPerQuarterNoteTimeDivision)(midiFile.TimeDivision)).TicksPerQuarterNote;
                str += "\tTicks per quater note: " + ticks + "\r\n";
                return str;
            }
            else if (midiFile.TimeDivision is SmpteTimeDivision)
            {
                string str = "";
                str += "Time division format: SMPTE\r\n";
                str += "\tFormat (frames per second): " + ((SmpteTimeDivision)(midiFile.TimeDivision)).Format + "\r\n";
                str += "\tResolution: " + ((SmpteTimeDivision)(midiFile.TimeDivision)).Resolution + "\r\n";
                return str;
            }
            else
            {
                throw new Exception("Can't detect time division format.\r\n");
            }
        }

        string GetNullCleansed(string s)
        {
            for(int i = 0; i < s.Length; i++)
            {
                if(s[i] == 0)
                {
                    s = s.Remove(i, 1);
                }
            }
            return s;
        }
    }
}
