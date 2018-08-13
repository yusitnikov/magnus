using System;
using System.Linq;

namespace Magnus
{
    struct Complex : IFormattable
    {
        public static readonly Complex Nil = new Complex(0, 0), One = new Complex(1, 0), I = new Complex(0, 1);

        public double Re, Im;

        public double Length => Misc.Hypot(Re, Im);
        public double SquareLength => Re * Re + Im * Im;

        public double Arg => Math.Atan2(Im, Re);

        public Complex Normal => this / Length;

        public Complex Conjugate => new Complex(Re, -Im);

        public Complex(double re, double im = 0)
        {
            Re = re;
            Im = im;
        }

        public static Complex Exp(Complex z)
        {
            return Math.Exp(z.Re) * new Complex(Math.Cos(z.Im), Math.Sin(z.Im));
        }

        // int (e^ix / x) dx = (Ci(x); Si(x)) + c
        public static Complex ExpIntOfImaginaryArg(double x)
        {
            // https://en.wikipedia.org/wiki/Trigonometric_integral#Efficient_evaluation
            double x2 = x * x, x4 = x2 * x2, x6 = x4 * x2, x8 = x6 * x2, x10 = x8 * x2, x12 = x10 * x2, x14 = x12 * x2, x16 = x14 * x2, x18 = x16 * x2, x20 = x18 * x2;
            if (Math.Abs(x) <= 4)
            {
                return new Complex(
                    Math.Log(Math.Abs(x)) + x2 * (-0.25 + 7.51851524438898291e-3 * x2 - 1.27528342240267686e-4 * x4 + 1.05297363846239184e-6 * x6 - 4.68889508144848019e-9 * x8 + 1.06480802891189243e-11 * x10 - 9.93728488857585407e-15 * x12)
                                     / (1 + 1.1592605689110735e-2 * x2 + 6.72126800814254432e-5 * x4 + 2.55533277086129636e-7 * x6 + 6.97071295760958946e-10 * x8 + 1.38536352772778619e-12 * x10 + 1.89106054713059759e-15 * x12 + 1.39759616731376855e-18 * x14),
                    x * (1 - 4.54393409816329991e-2 * x2 + 1.15457225751016682e-3 * x4 - 1.41018536821330254e-5 * x6 + 9.43280809438713025e-8 * x8 - 3.53201978997168357e-10 * x10 + 7.08240282274875911e-13 * x12 - 6.05338212010422477e-16 * x14)
                      / (1 + 1.01162145739225565e-2 * x2 + 4.99175116169755106e-5 * x4 + 1.55654986308745614e-7 * x6 + 3.28067571055789734e-10 * x8 + 4.5049097575386581e-13 * x10 + 3.21107051193712168e-16 * x12)
                );
            }
            else
            {
                var f = (1 + 7.44437068161936700618e2 / x2 + 1.96396372895146869801e5 / x4 + 2.37750310125431834034e7 / x6 + 1.43073403821274636888e9 / x8 + 4.33736238870432522765e10 / x10 + 6.40533830574022022911e11 / x12 + 4.20968180571076940208e12 / x14 + 1.00795182980368574617e13 / x16 + 4.94816688199951963482e12 / x18 - 4.94701168645415959931e11 / x20)
                      / (1 + 7.46437068161927678031e2 / x2 + 1.97865247031583951450e5 / x4 + 2.41535670165126845144e7 / x6 + 1.47478952192985464958e9 / x8 + 4.58595115847765779830e10 / x10 + 7.08501308149515401563e11 / x12 + 5.06084464593475076774e12 / x14 + 1.43468549171581016479e13 / x16 + 1.11535493509914254097e13 / x18)
                      / x;
                var g = (1 + 8.1359520115168615e2 / x2 + 2.35239181626478200e5 / x4 + 3.12557570795778731e7 / x6 + 2.06297595146763354e9 / x8 + 6.83052205423625007e10 / x10 + 1.09049528450362786e12 / x12 + 7.57664583257834349e12 / x14 + 1.81004487464664575e13 / x16 + 6.43291613143049485e12 / x18 - 1.36517137670871689e12 / x20)
                      / (1 + 8.19595201151451564e2 / x2 + 2.40036752835578777e5 / x4 + 3.26026661647090822e7 / x6 + 2.23355543278099360e9 / x8 + 7.87465017341829930e10 / x10 + 1.39866710696414565e12 / x12 + 1.17164723371736605e13 / x14 + 4.01839087307656620e13 / x16 + 3.99653257887490811e13 / x18)
                      / x2;
                var cos = Math.Cos(x);
                var sin = Math.Sin(x);
                return new Complex(
                    f * sin - g * cos,
                    - f * cos - g * sin
                );
            }
        }

        // int from x1 to x2 (e^ix / x) dx
        public static Complex ExpIntOfImaginaryArg(double x1, double x2)
        {
            return ExpIntOfImaginaryArg(x2) - ExpIntOfImaginaryArg(x1);
        }

        #region Operators

        public static Complex operator +(Complex z1, Complex z2)
        {
            return new Complex(z1.Re + z2.Re, z1.Im + z2.Im);
        }

        public static Complex operator -(Complex z1, Complex z2)
        {
            return new Complex(z1.Re - z2.Re, z1.Im - z2.Im);
        }

        public static Complex operator -(Complex z)
        {
            return new Complex(-z.Re, -z.Im);
        }

        public static Complex operator *(Complex z, double k)
        {
            return new Complex(z.Re * k, z.Im * k);
        }

        public static Complex operator *(double k, Complex z)
        {
            return new Complex(k * z.Re, k * z.Im);
        }

        public static Complex operator *(Complex z1, Complex z2)
        {
            return new Complex(z1.Re * z2.Re - z1.Im * z2.Im, z1.Re * z2.Im + z2.Re * z1.Im);
            // cos(a1+a2); sin(a1+a2)
        }

        public static Complex operator /(Complex z, double k)
        {
            return new Complex(z.Re / k, z.Im / k);
        }

        public static Complex operator /(double k, Complex z)
        {
            return z.Conjugate * (k / z.SquareLength);
        }

        public static Complex operator /(Complex z1, Complex z2)
        {
            return new Complex(z1.Re * z2.Re + z1.Im * z2.Im, z1.Im * z2.Re - z1.Re * z2.Im) / z2.SquareLength;
        }

        public static implicit operator Complex(double x)
        {
            return new Complex(x);
        }

        #endregion

        #region ToString

        private string toString(string reString, string imString)
        {
            if (Im == 0)
            {
                return reString;
            }
            else if (Re == 0)
            {
                return imString + "i";
            }
            else
            {
                return "(" + reString + (Im > 0 ? "+" : "") + imString + "i)";
            }
        }

        public override string ToString()
        {
            return toString(Re.ToString(), Im.ToString());
        }

        public string ToString(string format)
        {
            return toString(Re.ToString(format), Im.ToString(format));
        }

        public string ToString(IFormatProvider formatProvider)
        {
            return toString(Re.ToString(formatProvider), Im.ToString(formatProvider));
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            return toString(Re.ToString(format, formatProvider), Im.ToString(format, formatProvider));
        }

        #endregion
    }
}
