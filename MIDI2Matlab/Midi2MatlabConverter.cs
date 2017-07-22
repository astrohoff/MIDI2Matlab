using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Melanchall.DryWetMidi.Smf;

namespace MIDI2Matlab
{
    class Midi2MatlabConverter
    {
        // Deprecated I think ...?
        public static string GetFirstTrackNotesOnly(MidiFile midiFile)
        {
            string str = "";
            str += "voices(1).noteNumbers = [";
            TrackChunk firstTrack = (TrackChunk)midiFile.Chunks[1];
            bool firstNote = true;
            int noteCount = 0;
            for(int i = 0; i < firstTrack.Events.Count; i++)
            {
                if(firstTrack.Events[i] is NoteOnEvent)
                {
                    if (firstNote)
                    {
                        firstNote = false;
                    }
                    else 
                    {
                        str += ',';
                    }
                    int noteValue = ((NoteOnEvent)firstTrack.Events[i]).NoteNumber - 8;
                    str += noteValue;
                    noteCount++;
                }
            }
            str += "]\r\n";
            str += "voices(1).durations = [";
            for(int i = 0; i < noteCount; i++)
            {
                str += "1";
                if(i < noteCount - 1)
                {
                    str += ',';
                }
            }
            str += "]\r\n";
            str += "voices(1).startPulses = [";
            for(int i = 0; i < noteCount; i++)
            {
                str += (i + 1);
                if(i < noteCount - 1)
                {
                    str += ',';
                }
            }
            str += "]\r\n";
            return str;
        }

        // Deprecated I think ...?
        public static string GetFirstTrackWithTiming(MidiFile midiFile)
        {
            string str = "";
            // Get data values.
            List<int> noteValues = new List<int>();
            List<long> durrations = new List<long>();
            List<long> startPulses = new List<long>();
            TrackChunk firstTrack = (TrackChunk)midiFile.Chunks[1];
            long currentTick = 0;
            Console.WriteLine("NoteOff type: " + typeof(NoteOffEvent));
            for (int i = 0; i < firstTrack.Events.Count; i++)
            {
                if (firstTrack.Events[i] is NoteOnEvent)
                {
                    int midiNoteValue = ((NoteOnEvent)firstTrack.Events[i]).NoteNumber;
                    noteValues.Add(midiNoteValue - 8);
                    startPulses.Add(currentTick + 1);
                    // Find durration.
                    bool endFound = false;
                    int channel = ((NoteOnEvent)firstTrack.Events[i]).Channel;
                    long durration = 0;
                    for(int j = i + 1; j < i + 5 && !endFound; j++)
                    {
                        durration += firstTrack.Events[j].DeltaTime;
                        if (firstTrack.Events[j].GetType().Equals(typeof(NoteOffEvent)))
                        {
                            NoteOffEvent noteEvent = ((NoteOffEvent)firstTrack.Events[j]);
                            endFound = noteEvent.NoteNumber == midiNoteValue && noteEvent.Channel == channel;
                        }
                        else if (firstTrack.Events[j] is NoteOnEvent)
                        {
                            NoteOnEvent noteEvent = ((NoteOnEvent)firstTrack.Events[j]);
                            endFound = noteEvent.NoteNumber == midiNoteValue && noteEvent.Channel == channel;
                        }
                        
                        if (endFound)
                        {
                        }
                    }
                    durrations.Add(durration);
                }
                currentTick += firstTrack.Events[i].DeltaTime;
            }
            // Calulate pulse duration.
            double pulseDuration = 500000f / 96;
            if(midiFile.TimeDivision is TicksPerQuarterNoteTimeDivision)
            {
                double secondsPerQuarterNote = 0.000001 * GetTempo(midiFile);
                pulseDuration = secondsPerQuarterNote / ((TicksPerQuarterNoteTimeDivision)midiFile.TimeDivision).TicksPerQuarterNote;
            }
            else
            {
                throw new Exception("Unsuported time format.");
            }

            // Format data for matlab.
            str += "voices(1).noteNumbers = [";
            str += string.Join(",", Array.ConvertAll(noteValues.ToArray(), x => x.ToString())) + "]\r\n";
            str += "voices(1).durations = [";
            str += string.Join(",", Array.ConvertAll(durrations.ToArray(), x => x.ToString())) + "]\r\n";
            str += "voices(1).startPulses = [";
            str += string.Join(",", Array.ConvertAll(startPulses.ToArray(), x => x.ToString())) + "]\r\n";
            str += "pulseDuration = " + pulseDuration + "\r\n";
            return str;
        }

        public static string GetAllTracks(MidiFile midiFile)
        {
            string str = "";
            
            // Calulate pulse duration.
            double pulseDuration = 500000f / 96;
            if (midiFile.TimeDivision is TicksPerQuarterNoteTimeDivision)
            {
                double secondsPerQuarterNote = 0.000001 * GetTempo(midiFile);
                pulseDuration = secondsPerQuarterNote / ((TicksPerQuarterNoteTimeDivision)midiFile.TimeDivision).TicksPerQuarterNote;
            }
            else
            {
                throw new Exception("Unsuported time format.");
            }
            // Process each MIDI track.
            for (int trackIndex = 0; trackIndex < midiFile.Chunks.Count; trackIndex++)
            {
                // Get data values.
                List<int> noteValues = new List<int>();
                List<long> durrations = new List<long>();
                List<long> startPulses = new List<long>();
                List<float> velocities = new List<float>();
                StringBuilder vowelsBuilder = new StringBuilder();
                TrackChunk track = (TrackChunk)midiFile.Chunks[trackIndex];
                long currentTick = 0;
                Console.WriteLine("NoteOff type: " + typeof(NoteOffEvent));
                for (int i = 0; i < track.Events.Count; i++)
                {
                    if (track.Events[i] is NoteOnEvent)
                    {
                        // This is a new note.
                        int midiNoteValue = ((NoteOnEvent)track.Events[i]).NoteNumber;
                        noteValues.Add(midiNoteValue - 8);
                        float velocity = ((NoteOnEvent)track.Events[i]).Velocity / 127f;
                        velocities.Add(velocity);
                        // Set vowel based on midi channel.
                        vowelsBuilder.Append(GetVowel(((NoteOnEvent)track.Events[i]).Channel));
                        startPulses.Add(currentTick + track.Events[i].DeltaTime + 1);
                        // Find durration.
                        bool endFound = false;
                        int channel = ((NoteOnEvent)track.Events[i]).Channel;
                        long duration = 0;
                        for (int j = i + 1; j < i + 5 && !endFound; j++)
                        {
                            duration += track.Events[j].DeltaTime;
                            // Explicit note off.
                            if (track.Events[j].GetType().Equals(typeof(NoteOffEvent)))
                            {
                                NoteOffEvent noteEvent = ((NoteOffEvent)track.Events[j]);
                                endFound = noteEvent.NoteNumber == midiNoteValue && noteEvent.Channel == channel;

                                // This is a fix for a repeated note having a duration of 0.
                                // This is kind of hacky; the underlying cause of this problem should be
                                // investigated at some point.
                                if (endFound && duration == 0 && track.Events.Count > j + 1)
                                {
                                   duration += track.Events[j + 1].DeltaTime;
                                }
                            }
                            // Another note of the same value starting.
                            else if (track.Events[j] is NoteOnEvent)
                            {
                                NoteOnEvent noteEvent = ((NoteOnEvent)track.Events[j]);
                                endFound = noteEvent.NoteNumber == midiNoteValue && noteEvent.Channel == channel;
                            }
                        }
                        durrations.Add(duration);
                    }
                    currentTick += track.Events[i].DeltaTime;
                }
                // Format data for matlab.
                str += "voices(" + (trackIndex + 1) + ").noteNumbers = [";
                str += string.Join(",", Array.ConvertAll(noteValues.ToArray(), x => x.ToString())) + "]\r\n";
                str += "voices(" + (trackIndex + 1) + ").durations = [";
                str += string.Join(",", Array.ConvertAll(durrations.ToArray(), x => x.ToString())) + "]\r\n";
                str += "voices(" + (trackIndex + 1) + ").startPulses = [";
                str += string.Join(",", Array.ConvertAll(startPulses.ToArray(), x => x.ToString())) + "]\r\n";
                //str += "voices(" + (trackIndex + 1) + ").velocities = [";
                //str += string.Join(",", Array.ConvertAll(velocities.ToArray(), x => x.ToString())) + "]\r\n";
                str += "voices(" + (trackIndex + 1) + ").vowels = '" + vowelsBuilder.ToString() + "'\r\n";
            }
            str += "pulseDuration = " + pulseDuration + "\r\n";
            return str;
        }

        private static string GetVowel(FourBitNumber midiChannel)
        {
            switch (midiChannel)
            {
                case 0:
                    return "eh";
                case 1:
                    return "ee";
                case 2:
                    return "ah";
                case 3:
                    return "oh";
                case 4:
                    return "oo";
                default:
                    return "oh";
            }
        }

        private static long GetTempo(MidiFile midiFile)
        {
            bool found = false;
            long tempo = 0;
            for(int i = 0; i < midiFile.Chunks.Count; i++)
            {
                if(midiFile.Chunks[i] is TrackChunk)
                {
                    TrackChunk track = (TrackChunk)midiFile.Chunks[i];
                    for (int j = 0; j < track.Events.Count; j++)
                    {
                        if(track.Events[j] is SetTempoEvent)
                        {
                            if (found && tempo != ((SetTempoEvent)track.Events[j]).MicrosecondsPerBeat)
                            {
                                throw new Exception("Multiple tempos found.");
                            }
                            found = true;
                            tempo = ((SetTempoEvent)track.Events[j]).MicrosecondsPerBeat;
                        }
                    }
                }
            }
            return tempo;
        }
    }
}
