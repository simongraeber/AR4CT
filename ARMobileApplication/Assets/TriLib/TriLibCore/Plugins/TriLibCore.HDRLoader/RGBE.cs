using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Unity.Collections;

namespace HDRLoader
{
    /**
    * This file contains code to read and write four byte rgbe file format
    * developed by Greg Ward.  It handles the conversions between rgbe and
    * pixels consisting of floats.  The data is assumed to be an array of floats.
    * By default there are three floats per pixel in the order red, green, blue.
    * (RGBE_DATA_??? values control this.)  Only the mimimal header reading and
    * writing is implemented.  Each routine does error checking and will return
    * a status value as defined below.  This code is intended as a skeleton so
    * feel free to modify it to suit your needs.
    * <p>
    * Ported to Unity-C# by Ricardo Reis.
    * Ported to Java and restructured by Kenneth Russell.
    * posted to http://www.graphics.cornell.edu/~bjw/
    * written by Bruce Walter  (bjw@graphics.cornell.edu)  5/26/95
    * based on code written by Greg Ward
    * </p>
    *
    * @see <a href="https://java.net/projects/jogl-demos/sources/svn/content/trunk/src/demos/hdr/RGBE.java">Source</a>
    */
    internal class Rgbe
    {
        private const string ExposureString = "EXPOSURE=";

        private const string GammaString = "GAMMA=";
        private const int ValidExposure = 0x04;

        private const int ValidGamma = 0x02;

        // Flags indicating which fields in a Header are valid
        private const int ValidProgramtype = 0x01;

        private const string WidthHeightPattern = @"-Y (\d+) \+X (\d+)";

        public static Header ReadHeader(BinaryReader @in)
        {
            var valid = 0;
            string programType = null;
            var gamma = 1.0f;
            var exposure = 1.0f;
            var width = 0;
            var height = 0;

            var buf = @in.ReadLine();
            if (buf == null) throw new IOException("Unexpected EOF reading magic token");
            if (buf[0] == '#' && buf[1] == '?')
            {
                valid |= ValidProgramtype;
                programType = buf.Substring(2);
                buf = @in.ReadLine();
                if (buf == null) throw new IOException("Unexpected EOF reading line after magic token");
            }

            var foundFormat = false;
            var done = false;
            while (!done)
            {
                if (buf.Equals("FORMAT=32-bit_rle_rgbe"))
                {
                    foundFormat = true;
                }
                else if (buf.StartsWith(GammaString))
                {
                    valid |= ValidGamma;
                    gamma = Convert.ToSingle(buf.Substring(GammaString.Length), CultureInfo.InvariantCulture);
                }
                else if (buf.StartsWith(ExposureString))
                {
                    valid |= ValidExposure;
                    exposure = Convert.ToSingle(buf.Substring(ExposureString.Length), CultureInfo.InvariantCulture);
                }
                else
                {
                    var m = Regex.Match(buf, WidthHeightPattern, RegexOptions.IgnoreCase);
                    if (m.Success)
                    {
                        width = Convert.ToInt32(m.Groups[2].Value);
                        height = Convert.ToInt32(m.Groups[1].Value);
                        done = true;
                    }
                }

                if (!done)
                {
                    buf = @in.ReadLine();
                    if (buf == null) throw new IOException("Unexpected EOF reading header");
                }
            }

            if (!foundFormat) throw new IOException("No FORMAT specifier found");

            return new Header(valid, programType, gamma, exposure, width, height);
        }

        private static void ReadPixelsRaw(BinaryReader @in, byte[] data, int offset, int numpixels)
        {
            var numExpected = 4 * numpixels;
            @in.ReadFully(data, offset, numExpected);
        }

        public static void ReadPixelsRawRle(BinaryReader @in, byte[] data, int offset, int scanline_width, int num_scanlines)
        {
            var rgbe = new byte[4];
            byte[] scanline_buffer = null;
            int ptr, ptr_end;
            int count;
            var buf = new byte[2];

            if (scanline_width < 8 || scanline_width > 0x7fff)
            {
                // run length encoding is not allowed so read flat
                ReadPixelsRaw(@in, data, offset, scanline_width * num_scanlines);
            }

            // read in each successive scanline
            while (num_scanlines > 0)
            {
                @in.ReadFully(rgbe);

                if (rgbe[0] != 2 || rgbe[1] != 2 || (rgbe[2] & 0x80) != 0)
                {
                    data[offset++] = rgbe[0];
                    data[offset++] = rgbe[1];
                    data[offset++] = rgbe[2];
                    data[offset++] = rgbe[3];
                    // this file is not run length encoded
                    ReadPixelsRaw(@in, rgbe, offset, scanline_width * num_scanlines - 1);
                }

                if ((((rgbe[2] & 0xFF) << 8) | (rgbe[3] & 0xFF)) != scanline_width)
                {
                    throw new IOException("Wrong scanline width " +
                                          (((rgbe[2] & 0xFF) << 8) | (rgbe[3] & 0xFF)) +
                                          ", expected " + scanline_width);
                }

                if (scanline_buffer == null)
                {
                    scanline_buffer = new byte[4 * scanline_width];
                }

                ptr = 0;
                // read each of the four channels for the scanline into the buffer
                for (var i = 0; i < 4; i++)
                {
                    ptr_end = (i + 1) * scanline_width;
                    while (ptr < ptr_end)
                    {
                        @in.ReadFully(buf);

                        if ((buf[0] & 0xFF) > 128)
                        {
                            // a run of the same value
                            count = (buf[0] & 0xFF) - 128;
                            if (count == 0 || count > ptr_end - ptr)
                            {
                                throw new IOException("Bad scanline data");
                            }

                            while (count-- > 0) scanline_buffer[ptr++] = buf[1];
                        }
                        else
                        {
                            // a non-run
                            count = buf[0] & 0xFF;
                            if (count == 0 || count > ptr_end - ptr)
                            {
                                throw new IOException("Bad scanline data");
                            }

                            scanline_buffer[ptr++] = buf[1];
                            if (--count > 0)
                            {
                                @in.ReadFully(scanline_buffer, ptr, count);
                                ptr += count;
                            }
                        }
                    }
                }

                // copy byte data to output
                for (var i = 0; i < scanline_width; i++)
                {
                    data[offset++] = scanline_buffer[i];
                    data[offset++] = scanline_buffer[i + scanline_width];
                    data[offset++] = scanline_buffer[i + 2 * scanline_width];
                    data[offset++] = scanline_buffer[i + 3 * scanline_width];
                }

                num_scanlines--;
            }
        }

        /**
     * Standard conversion from rgbe to float pixels.  Note: Ward uses
     * ldexp(col+0.5,exp-(128+8)). However we wanted pixels in the
     * range [0,1] to map back into the range [0,1].
     */
        public void Rgbe2Float(NativeArray<float> rgba, byte[] rgbe, int rgbeIndex, int rgbaIndex)
        {
            if (rgbe[rgbeIndex + 3] != 0)
            {
                // nonzero pixel
                var f = UnityEngine.Mathf.Pow(2f, (rgbe[rgbeIndex + 3] & 0xFF) - (128 + 8));
                var r = (rgbe[rgbeIndex + 0] & 0xFF) * f;
                var g = (rgbe[rgbeIndex + 1] & 0xFF) * f;
                var b = (rgbe[rgbeIndex + 2] & 0xFF) * f;
                var a = 1f;
                var color = new UnityEngine.Color(r, g, b, a);
                var gamma = color.gamma;
                rgba[rgbaIndex + 0] = gamma.r;
                rgba[rgbaIndex + 1] = gamma.g;
                rgba[rgbaIndex + 2] = gamma.b;
                rgba[rgbaIndex + 3] = gamma.a;
            }
            else
            {
                rgba[rgbaIndex + 0] = 0;
                rgba[rgbaIndex + 1] = 0;
                rgba[rgbaIndex + 2] = 0;
                rgba[rgbaIndex + 3] = 1f;
            }
        }

        internal class Header
        {
            private readonly int _valid;

            public Header(int valid,
                string programType,
                float gamma,
                float exposure,
                int width,
                int height)
            {
                this._valid = valid;
                this.ProgramType = programType;
                this.Gamma = gamma;
                this.Exposure = exposure;
                this.Width = width;
                this.Height = height;
            }

            public float Exposure { get; }

            public float Gamma { get; }

            public int Height { get; }

            public string ProgramType { get; }

            public int Width { get; }

            public bool IsExposureValid => (_valid & ValidExposure) != 0;

            public bool IsGammaValid => (_valid & ValidGamma) != 0;

            public bool IsProgramTypeValid => (_valid & ValidProgramtype) != 0;

            public override string ToString()
            {
                var buf = new StringBuilder();
                if (IsProgramTypeValid)
                {
                    buf.Append(" Program type: ");
                    buf.Append(ProgramType);
                }

                buf.Append(" Gamma");
                if (IsGammaValid) buf.Append(" [valid]");
                buf.Append(": ");
                buf.Append(Gamma);
                buf.Append(" Exposure");
                if (IsExposureValid) buf.Append(" [valid]");
                buf.Append(": ");
                buf.Append(Exposure);
                buf.Append(" Width: ");
                buf.Append(Width);
                buf.Append(" Height: ");
                buf.Append(Height);
                return buf.ToString();
            }
        }
    }
}