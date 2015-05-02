using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace arduinoCameraStream
{
    class decoding
    {
        static float CLIP(float X)
        {
            return (X) > 255 ? 255 : (X) < 0 ? 0 : X;
        }
        unsafe static void MalvarDemosaic(float[] Out, float[] Input, int Width, int Height, int RedX, int RedY)
        {//I do not take credit for this function the code is from http://www.ipol.im/pub/art/2011/g_mhcd/
            int BlueX = 1 - RedX;
            int BlueY = 1 - RedY;
            fixed (float* Output = &Out[0])
            {
                float* OutputRed = Output;
                float* OutputGreen = Output + Width * Height;
                float* OutputBlue = Output + 2 * Width * Height;
                /* Neigh holds a copy of the 5x5 neighborhood around the current point */
                float[,] Neigh = new float[5, 5];
                /* NeighPresence is used for boundary handling.  It is set to 0 if the 
                   neighbor is beyond the boundaries of the image and 1 otherwise. */
                int[,] NeighPresence = new int[5, 5];
                int i, j, x, y, nx, ny;


                for (y = 0, i = 0; y < Height; y++)
                {
                    for (x = 0; x < Width; x++, i++)
                    {
                        /* 5x5 neighborhood around the point (x,y) is copied into Neigh */
                        for (ny = -2, j = x + Width * (y - 2); ny <= 2; ny++, j += Width)
                        {
                            for (nx = -2; nx <= 2; nx++)
                            {
                                if (0 <= x + nx && x + nx < Width
                                        && 0 <= y + ny && y + ny < Height)
                                {
                                    Neigh[2 + nx, 2 + ny] = Input[j + nx];
                                    NeighPresence[2 + nx, 2 + ny] = 1;
                                }
                                else
                                {
                                    Neigh[2 + nx, 2 + ny] = 0;
                                    NeighPresence[2 + nx, 2 + ny] = 0;
                                }
                            }
                        }

                        if ((x & 1) == RedX && (y & 1) == RedY)
                        {
                            /* Center pixel is red */
                            OutputRed[i] = Input[i];
                            OutputGreen[i] = (2 * (Neigh[2, 1] + Neigh[1, 2]
                                        + Neigh[3, 2] + Neigh[2, 3])
                                    + (NeighPresence[0, 2] + NeighPresence[4, 2]
                                        + NeighPresence[2, 0] + NeighPresence[2, 4]) * Neigh[2, 2]
                                    - Neigh[0, 2] - Neigh[4, 2]
                                    - Neigh[2, 0] - Neigh[2, 4])
                                / (2 * (NeighPresence[2, 1] + NeighPresence[1, 2]
                                            + NeighPresence[3, 2] + NeighPresence[2, 3]));
                            OutputBlue[i] = (4 * (Neigh[1, 1] + Neigh[3, 1]
                                        + Neigh[1, 3] + Neigh[3, 3]) +
                                    3 * ((NeighPresence[0, 2] + NeighPresence[4, 2]
                                            + NeighPresence[2, 0] + NeighPresence[2, 4]) * Neigh[2, 2]
                                        - Neigh[0, 2] - Neigh[4, 2]
                                        - Neigh[2, 0] - Neigh[2, 4]))
                                / (4 * (NeighPresence[1, 1] + NeighPresence[3, 1]
                                            + NeighPresence[1, 3] + NeighPresence[3, 3]));
                        }
                        else if ((x & 1) == BlueX && (y & 1) == BlueY)
                        {
                            /* Center pixel is blue */
                            OutputBlue[i] = Input[i];
                            OutputGreen[i] = (2 * (Neigh[2, 1] + Neigh[1, 2]
                                        + Neigh[3, 2] + Neigh[2, 3])
                                    + (NeighPresence[0, 2] + NeighPresence[4, 2]
                                        + NeighPresence[2, 0] + NeighPresence[2, 4]) * Neigh[2, 2]
                                    - Neigh[0, 2] - Neigh[4, 2]
                                    - Neigh[2, 0] - Neigh[2, 4])
                                / (2 * (NeighPresence[2, 1] + NeighPresence[1, 2]
                                            + NeighPresence[3, 2] + NeighPresence[2, 3]));
                            OutputRed[i] = (4 * (Neigh[1, 1] + Neigh[3, 1]
                                        + Neigh[1, 3] + Neigh[3, 3]) +
                                    3 * ((NeighPresence[0, 2] + NeighPresence[4, 2]
                                            + NeighPresence[2, 0] + NeighPresence[2, 4]) * Neigh[2, 2]
                                        - Neigh[0, 2] - Neigh[4, 2]
                                        - Neigh[2, 0] - Neigh[2, 4]))
                                / (4 * (NeighPresence[1, 1] + NeighPresence[3, 1]
                                            + NeighPresence[1, 3] + NeighPresence[3, 3]));
                        }
                        else
                        {
                            /* Center pixel is green */
                            OutputGreen[i] = Input[i];

                            if ((y & 1) == RedY)
                            {
                                /* Left and right neighbors are red */
                                OutputRed[i] = (8 * (Neigh[1, 2] + Neigh[3, 2])
                                        + (2 * (NeighPresence[1, 1] + NeighPresence[3, 1]
                                                + NeighPresence[0, 2] + NeighPresence[4, 2]
                                                + NeighPresence[1, 3] + NeighPresence[3, 3])
                                            - NeighPresence[2, 0] - NeighPresence[2, 4]) * Neigh[2, 2]
                                        - 2 * (Neigh[1, 1] + Neigh[3, 1]
                                            + Neigh[0, 2] + Neigh[4, 2]
                                            + Neigh[1, 3] + Neigh[3, 3])
                                        + Neigh[2, 0] + Neigh[2, 4])
                                    / (8 * (NeighPresence[1, 2] + NeighPresence[3, 2]));
                                OutputBlue[i] = (8 * (Neigh[2, 1] + Neigh[2, 3])
                                        + (2 * (NeighPresence[1, 1] + NeighPresence[3, 1]
                                                + NeighPresence[2, 0] + NeighPresence[2, 4]
                                                + NeighPresence[1, 3] + NeighPresence[3, 3])
                                            - NeighPresence[0, 2] - NeighPresence[4, 2]) * Neigh[2, 2]
                                        - 2 * (Neigh[1, 1] + Neigh[3, 1]
                                            + Neigh[2, 0] + Neigh[2, 4]
                                            + Neigh[1, 3] + Neigh[3, 3])
                                        + Neigh[0, 2] + Neigh[4, 2])
                                    / (8 * (NeighPresence[2, 1] + NeighPresence[2, 3]));
                            }
                            else
                            {
                                /* Left and right neighbors are blue */
                                OutputRed[i] = (8 * (Neigh[2, 1] + Neigh[2, 3])
                                        + (2 * (NeighPresence[1, 1] + NeighPresence[3, 1]
                                                + NeighPresence[2, 0] + NeighPresence[2, 4]
                                                + NeighPresence[1, 3] + NeighPresence[3, 3])
                                            - NeighPresence[0, 2] - NeighPresence[4, 2]) * Neigh[2, 2]
                                        - 2 * (Neigh[1, 1] + Neigh[3, 1]
                                            + Neigh[2, 0] + Neigh[2, 4]
                                            + Neigh[1, 3] + Neigh[3, 3])
                                        + Neigh[0, 2] + Neigh[4, 2])
                                    / (8 * (NeighPresence[2, 1] + NeighPresence[2, 3]));
                                OutputBlue[i] = (8 * (Neigh[1, 2] + Neigh[3, 2])
                                        + (2 * (NeighPresence[1, 1] + NeighPresence[3, 1]
                                                + NeighPresence[0, 2] + NeighPresence[4, 2]
                                                + NeighPresence[1, 3] + NeighPresence[3, 3])
                                            - NeighPresence[2, 0] - NeighPresence[2, 4]) * Neigh[2, 2]
                                        - 2 * (Neigh[1, 1] + Neigh[3, 1]
                                            + Neigh[0, 2] + Neigh[4, 2]
                                            + Neigh[1, 3] + Neigh[3, 3])
                                        + Neigh[2, 0] + Neigh[2, 4])
                                    / (8 * (NeighPresence[1, 2] + NeighPresence[3, 2]));
                            }
                        }
                    }
                }
            }
        }
        static public void deBayerHQl(byte[] In, byte[] Out)
        {
            int img_w = 640;
            int img_w_2 = img_w*2;
            int img_w_3 = img_w*3;
            int img_h = 480;
            float[] inf = new float[img_w * img_h];
            float[] outf = new float[img_w_3 * img_h];
            int z;
            for (z = 0; z < img_w * img_h; ++z)
                inf[z] = In[z];
            MalvarDemosaic(outf, inf, img_w, img_h, 1, 1);
            for (z = 0; z < img_w_3 * img_h; z += 3)
            {
                Out[z] = System.Convert.ToByte(CLIP(outf[z / 3]));
                Out[z + 1] = System.Convert.ToByte(CLIP(outf[(z / 3) + (img_w * img_h)]));
                Out[z + 2] = System.Convert.ToByte(CLIP(outf[(z / 3) + (img_w_2 * img_h)]));
            }
        }
    }
}
