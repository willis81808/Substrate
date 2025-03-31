using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Substrate.Utils.CodecPattern
{
    // Builder starter
    public static class RecordCodecBuilder
    {
        public static CodecBuilder<T> Create<T>()
        {
            return new CodecBuilder<T>();
        }
    }

    // Base builder class
    public class CodecBuilder<T>
    {
        // Apply method to start building with type inference
        public CodecBuilder<T, P1> Apply<P1>(
            ICodec<P1> codec,
            string name,
            Expression<Func<T, P1>> getter)
        {
            var field = new FieldCodec<T, P1>(codec, name, getter.Compile());
            return new CodecBuilder<T, P1>(field);
        }
    }

    // Generated builder with 1 field
    public class CodecBuilder<T, P1>
    {
        private readonly FieldCodec<T, P1> field1;

        public CodecBuilder(FieldCodec<T, P1> field1)
        {
            this.field1 = field1;
        }

        public CodecBuilder<T, P1, P2> Apply<P2>(
            ICodec<P2> codec,
            string name,
            Expression<Func<T, P2>> getter)
        {
            var field2 = new FieldCodec<T, P2>(codec, name, getter.Compile());
            return new CodecBuilder<T, P1, P2>(field1, field2);
        }

        public ICodec<T> Build(Func<P1, T> constructor)
        {
            return new ObjectCodec<T>(new List<object> { field1 }, constructor);
        }
    }

    // Generated builder with 2 fields
    public class CodecBuilder<T, P1, P2>
    {
        private readonly FieldCodec<T, P1> field1;
        private readonly FieldCodec<T, P2> field2;

        public CodecBuilder(FieldCodec<T, P1> field1, FieldCodec<T, P2> field2)
        {
            this.field1 = field1;
            this.field2 = field2;
        }

        public CodecBuilder<T, P1, P2, P3> Apply<P3>(
            ICodec<P3> codec,
            string name,
            Expression<Func<T, P3>> getter)
        {
            var field3 = new FieldCodec<T, P3>(codec, name, getter.Compile());
            return new CodecBuilder<T, P1, P2, P3>(field1, field2, field3);
        }

        public ICodec<T> Build(Func<P1, P2, T> constructor)
        {
            return new ObjectCodec<T>(new List<object> { field1, field2 }, constructor);
        }
    }

    // Generated builder with 3 fields
    public class CodecBuilder<T, P1, P2, P3>
    {
        private readonly FieldCodec<T, P1> field1;
        private readonly FieldCodec<T, P2> field2;
        private readonly FieldCodec<T, P3> field3;

        public CodecBuilder(FieldCodec<T, P1> field1, FieldCodec<T, P2> field2, FieldCodec<T, P3> field3)
        {
            this.field1 = field1;
            this.field2 = field2;
            this.field3 = field3;
        }

        public CodecBuilder<T, P1, P2, P3, P4> Apply<P4>(
            ICodec<P4> codec,
            string name,
            Expression<Func<T, P4>> getter)
        {
            var field4 = new FieldCodec<T, P4>(codec, name, getter.Compile());
            return new CodecBuilder<T, P1, P2, P3, P4>(field1, field2, field3, field4);
        }

        public ICodec<T> Build(Func<P1, P2, P3, T> constructor)
        {
            return new ObjectCodec<T>(new List<object> { field1, field2, field3 }, constructor);
        }
    }

    // Generated builder with 4 fields
    public class CodecBuilder<T, P1, P2, P3, P4>
    {
        private readonly FieldCodec<T, P1> field1;
        private readonly FieldCodec<T, P2> field2;
        private readonly FieldCodec<T, P3> field3;
        private readonly FieldCodec<T, P4> field4;

        public CodecBuilder(FieldCodec<T, P1> field1, FieldCodec<T, P2> field2, FieldCodec<T, P3> field3, FieldCodec<T, P4> field4)
        {
            this.field1 = field1;
            this.field2 = field2;
            this.field3 = field3;
            this.field4 = field4;
        }

        public CodecBuilder<T, P1, P2, P3, P4, P5> Apply<P5>(
            ICodec<P5> codec,
            string name,
            Expression<Func<T, P5>> getter)
        {
            var field5 = new FieldCodec<T, P5>(codec, name, getter.Compile());
            return new CodecBuilder<T, P1, P2, P3, P4, P5>(field1, field2, field3, field4, field5);
        }

        public ICodec<T> Build(Func<P1, P2, P3, P4, T> constructor)
        {
            return new ObjectCodec<T>(new List<object> { field1, field2, field3, field4 }, constructor);
        }
    }

    // Generated builder with 5 fields
    public class CodecBuilder<T, P1, P2, P3, P4, P5>
    {
        private readonly FieldCodec<T, P1> field1;
        private readonly FieldCodec<T, P2> field2;
        private readonly FieldCodec<T, P3> field3;
        private readonly FieldCodec<T, P4> field4;
        private readonly FieldCodec<T, P5> field5;

        public CodecBuilder(FieldCodec<T, P1> field1, FieldCodec<T, P2> field2, FieldCodec<T, P3> field3, FieldCodec<T, P4> field4, FieldCodec<T, P5> field5)
        {
            this.field1 = field1;
            this.field2 = field2;
            this.field3 = field3;
            this.field4 = field4;
            this.field5 = field5;
        }

        public CodecBuilder<T, P1, P2, P3, P4, P5, P6> Apply<P6>(
            ICodec<P6> codec,
            string name,
            Expression<Func<T, P6>> getter)
        {
            var field6 = new FieldCodec<T, P6>(codec, name, getter.Compile());
            return new CodecBuilder<T, P1, P2, P3, P4, P5, P6>(field1, field2, field3, field4, field5, field6);
        }

        public ICodec<T> Build(Func<P1, P2, P3, P4, P5, T> constructor)
        {
            return new ObjectCodec<T>(new List<object> { field1, field2, field3, field4, field5 }, constructor);
        }
    }

    // Generated builder with 6 fields
    public class CodecBuilder<T, P1, P2, P3, P4, P5, P6>
    {
        private readonly FieldCodec<T, P1> field1;
        private readonly FieldCodec<T, P2> field2;
        private readonly FieldCodec<T, P3> field3;
        private readonly FieldCodec<T, P4> field4;
        private readonly FieldCodec<T, P5> field5;
        private readonly FieldCodec<T, P6> field6;

        public CodecBuilder(FieldCodec<T, P1> field1, FieldCodec<T, P2> field2, FieldCodec<T, P3> field3, FieldCodec<T, P4> field4, FieldCodec<T, P5> field5, FieldCodec<T, P6> field6)
        {
            this.field1 = field1;
            this.field2 = field2;
            this.field3 = field3;
            this.field4 = field4;
            this.field5 = field5;
            this.field6 = field6;
        }

        public CodecBuilder<T, P1, P2, P3, P4, P5, P6, P7> Apply<P7>(
            ICodec<P7> codec,
            string name,
            Expression<Func<T, P7>> getter)
        {
            var field7 = new FieldCodec<T, P7>(codec, name, getter.Compile());
            return new CodecBuilder<T, P1, P2, P3, P4, P5, P6, P7>(field1, field2, field3, field4, field5, field6, field7);
        }

        public ICodec<T> Build(Func<P1, P2, P3, P4, P5, P6, T> constructor)
        {
            return new ObjectCodec<T>(new List<object> { field1, field2, field3, field4, field5, field6 }, constructor);
        }
    }

    // Generated builder with 7 fields
    public class CodecBuilder<T, P1, P2, P3, P4, P5, P6, P7>
    {
        private readonly FieldCodec<T, P1> field1;
        private readonly FieldCodec<T, P2> field2;
        private readonly FieldCodec<T, P3> field3;
        private readonly FieldCodec<T, P4> field4;
        private readonly FieldCodec<T, P5> field5;
        private readonly FieldCodec<T, P6> field6;
        private readonly FieldCodec<T, P7> field7;

        public CodecBuilder(FieldCodec<T, P1> field1, FieldCodec<T, P2> field2, FieldCodec<T, P3> field3, FieldCodec<T, P4> field4, FieldCodec<T, P5> field5, FieldCodec<T, P6> field6, FieldCodec<T, P7> field7)
        {
            this.field1 = field1;
            this.field2 = field2;
            this.field3 = field3;
            this.field4 = field4;
            this.field5 = field5;
            this.field6 = field6;
            this.field7 = field7;
        }

        public CodecBuilder<T, P1, P2, P3, P4, P5, P6, P7, P8> Apply<P8>(
            ICodec<P8> codec,
            string name,
            Expression<Func<T, P8>> getter)
        {
            var field8 = new FieldCodec<T, P8>(codec, name, getter.Compile());
            return new CodecBuilder<T, P1, P2, P3, P4, P5, P6, P7, P8>(field1, field2, field3, field4, field5, field6, field7, field8);
        }

        public ICodec<T> Build(Func<P1, P2, P3, P4, P5, P6, P7, T> constructor)
        {
            return new ObjectCodec<T>(new List<object> { field1, field2, field3, field4, field5, field6, field7 }, constructor);
        }
    }

    // Generated builder with 8 fields
    public class CodecBuilder<T, P1, P2, P3, P4, P5, P6, P7, P8>
    {
        private readonly FieldCodec<T, P1> field1;
        private readonly FieldCodec<T, P2> field2;
        private readonly FieldCodec<T, P3> field3;
        private readonly FieldCodec<T, P4> field4;
        private readonly FieldCodec<T, P5> field5;
        private readonly FieldCodec<T, P6> field6;
        private readonly FieldCodec<T, P7> field7;
        private readonly FieldCodec<T, P8> field8;

        public CodecBuilder(FieldCodec<T, P1> field1, FieldCodec<T, P2> field2, FieldCodec<T, P3> field3, FieldCodec<T, P4> field4, FieldCodec<T, P5> field5, FieldCodec<T, P6> field6, FieldCodec<T, P7> field7, FieldCodec<T, P8> field8)
        {
            this.field1 = field1;
            this.field2 = field2;
            this.field3 = field3;
            this.field4 = field4;
            this.field5 = field5;
            this.field6 = field6;
            this.field7 = field7;
            this.field8 = field8;
        }

        public CodecBuilder<T, P1, P2, P3, P4, P5, P6, P7, P8, P9> Apply<P9>(
            ICodec<P9> codec,
            string name,
            Expression<Func<T, P9>> getter)
        {
            var field9 = new FieldCodec<T, P9>(codec, name, getter.Compile());
            return new CodecBuilder<T, P1, P2, P3, P4, P5, P6, P7, P8, P9>(field1, field2, field3, field4, field5, field6, field7, field8, field9);
        }

        public ICodec<T> Build(Func<P1, P2, P3, P4, P5, P6, P7, P8, T> constructor)
        {
            return new ObjectCodec<T>(new List<object> { field1, field2, field3, field4, field5, field6, field7, field8 }, constructor);
        }
    }

    // Generated builder with 9 fields
    public class CodecBuilder<T, P1, P2, P3, P4, P5, P6, P7, P8, P9>
    {
        private readonly FieldCodec<T, P1> field1;
        private readonly FieldCodec<T, P2> field2;
        private readonly FieldCodec<T, P3> field3;
        private readonly FieldCodec<T, P4> field4;
        private readonly FieldCodec<T, P5> field5;
        private readonly FieldCodec<T, P6> field6;
        private readonly FieldCodec<T, P7> field7;
        private readonly FieldCodec<T, P8> field8;
        private readonly FieldCodec<T, P9> field9;

        public CodecBuilder(FieldCodec<T, P1> field1, FieldCodec<T, P2> field2, FieldCodec<T, P3> field3, FieldCodec<T, P4> field4, FieldCodec<T, P5> field5, FieldCodec<T, P6> field6, FieldCodec<T, P7> field7, FieldCodec<T, P8> field8, FieldCodec<T, P9> field9)
        {
            this.field1 = field1;
            this.field2 = field2;
            this.field3 = field3;
            this.field4 = field4;
            this.field5 = field5;
            this.field6 = field6;
            this.field7 = field7;
            this.field8 = field8;
            this.field9 = field9;
        }

        public CodecBuilder<T, P1, P2, P3, P4, P5, P6, P7, P8, P9, P10> Apply<P10>(
            ICodec<P10> codec,
            string name,
            Expression<Func<T, P10>> getter)
        {
            var field10 = new FieldCodec<T, P10>(codec, name, getter.Compile());
            return new CodecBuilder<T, P1, P2, P3, P4, P5, P6, P7, P8, P9, P10>(field1, field2, field3, field4, field5, field6, field7, field8, field9, field10);
        }

        public ICodec<T> Build(Func<P1, P2, P3, P4, P5, P6, P7, P8, P9, T> constructor)
        {
            return new ObjectCodec<T>(new List<object> { field1, field2, field3, field4, field5, field6, field7, field8, field9 }, constructor);
        }
    }

    // Generated builder with 10 fields
    public class CodecBuilder<T, P1, P2, P3, P4, P5, P6, P7, P8, P9, P10>
    {
        private readonly FieldCodec<T, P1> field1;
        private readonly FieldCodec<T, P2> field2;
        private readonly FieldCodec<T, P3> field3;
        private readonly FieldCodec<T, P4> field4;
        private readonly FieldCodec<T, P5> field5;
        private readonly FieldCodec<T, P6> field6;
        private readonly FieldCodec<T, P7> field7;
        private readonly FieldCodec<T, P8> field8;
        private readonly FieldCodec<T, P9> field9;
        private readonly FieldCodec<T, P10> field10;

        public CodecBuilder(FieldCodec<T, P1> field1, FieldCodec<T, P2> field2, FieldCodec<T, P3> field3, FieldCodec<T, P4> field4, FieldCodec<T, P5> field5, FieldCodec<T, P6> field6, FieldCodec<T, P7> field7, FieldCodec<T, P8> field8, FieldCodec<T, P9> field9, FieldCodec<T, P10> field10)
        {
            this.field1 = field1;
            this.field2 = field2;
            this.field3 = field3;
            this.field4 = field4;
            this.field5 = field5;
            this.field6 = field6;
            this.field7 = field7;
            this.field8 = field8;
            this.field9 = field9;
            this.field10 = field10;
        }

        public ICodec<T> Build(Func<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, T> constructor)
        {
            return new ObjectCodec<T>(new List<object> { field1, field2, field3, field4, field5, field6, field7, field8, field9, field10 }, constructor);
        }
    }
}
