// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Globalization;
using System.IO;
using System.Text;
using osu.Game.IO;
using osu.Game.IO.Serialization;
using Newtonsoft.Json;
using osu.Game.Beatmaps.Formats;

namespace osu.Game.Skinning
{
    public class LegacySkinDecoder : LegacyDecoder<SkinConfiguration>
    {
        public LegacySkinDecoder()
            : base(1)
        {
        }

        protected override void ParseStreamInto(LineBufferedReader stream, SkinConfiguration output)
        {
            if (stream.PeekLine()?.Trim() != VibBeatmapEncoder.MAGIC)
            {
                base.ParseStreamInto(stream, output);
                return;
            }
            stream.ReadLine();
            var document = JsonConvert.DeserializeObject<VibDocument>(stream.ReadToEnd(), JsonSerializableExtensions.CreateGlobalSettings())
                ?? throw new InvalidDataException("Missing native beatmap data.");
            using var payload = new MemoryStream(Encoding.UTF8.GetBytes(document.Legacy));
            using var reader = new LineBufferedReader(payload);
            base.ParseStreamInto(reader, output);
        }

        protected override void ParseLine(SkinConfiguration skin, Section section, string line)
        {
            if (section != Section.Colours)
            {
                var pair = SplitKeyVal(line);

                switch (section)
                {
                    case Section.General:
                        switch (pair.Key)
                        {
                            case @"Name":
                                skin.SkinInfo.Name = pair.Value;
                                return;

                            case @"Author":
                                skin.SkinInfo.Creator = pair.Value;
                                return;

                            case @"Version":
                                if (pair.Value == "latest")
                                    skin.LegacyVersion = SkinConfiguration.LATEST_VERSION;
                                else if (decimal.TryParse(pair.Value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal version))
                                    skin.LegacyVersion = version;

                                return;
                        }

                        break;

                    // osu!catch section only has colour settings
                    // so no harm in handling the entire section
                    case Section.CatchTheBeat:
                        HandleColours(skin, line, true);
                        return;
                }

                if (!string.IsNullOrEmpty(pair.Key))
                    skin.ConfigDictionary[pair.Key] = pair.Value;
            }

            base.ParseLine(skin, section, line);
        }

        protected override SkinConfiguration CreateTemplateObject()
        {
            var config = base.CreateTemplateObject();
            config.LegacyVersion = 1.0m;
            return config;
        }
    }
}
