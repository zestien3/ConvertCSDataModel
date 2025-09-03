// *************************************
// Demo classes to be used in Demo Mode.
//
// The application will open the
// executing assembly and look for all
// classes having the [UseInFrontend]
// attribute set on them. Since it only
// checks for the name of the attribute,
// you can easily create your own empty
// attribute class like in this file.
// *************************************

using System;
using System.Collections.Generic;

namespace Zestien3
{
    /// <summary>
    /// Attribute which tells the application to process the class it is set on.
    /// </summary>
    internal class UseInFrontendAttribute : Attribute
    {
        public string? SubFolder { get; set; }
    }

    /// <summary>
    /// Enumeration class for the demo of this application
    /// </summary>
    [UseInFrontend(SubFolder = "Demo\\Level1")]
    public enum EnumDemo
    {
        /// <summary>
        /// EnumDemo First value
        /// </summary>
        FirstValue,

        /// <summary>
        /// EnumDemo Second value
        /// </summary>
        SecondValue
    }

    /// <summary>
    /// Main class for the demo of this application.
    /// </summary>
    /// <remarks>
    /// It contains all available standard types.
    /// </remarks>
    [UseInFrontend(SubFolder = "Demo")]
    public class AllStandardTypes
    {
        /// <summary>
        /// Property of type bool
        /// </summary>
        public bool BoolProperty { get; set; }

        /// <summary>
        /// Property of type sbyte
        /// </summary>
        public sbyte SByteProperty { get; set; }

        /// <summary>
        /// Property of type byte
        /// </summary>
        public byte ByteProperty { get; set; }

        /// <summary>
        /// Property of type char
        /// </summary>
        public char CharProperty { get; set; }

        /// <summary>
        /// Property of type Int16
        /// </summary>
        public Int16 Int16Property { get; set; }

        /// <summary>
        /// Property of type UInt16
        /// </summary>
        public UInt16 UInt16Property { get; set; }

        /// <summary>
        /// Property of type Int32
        /// </summary>
        public Int32 Int32Property { get; set; }

        /// <summary>
        /// Property of type UInt32
        /// </summary>
        public UInt32 UInt32Property { get; set; }

        /// <summary>
        /// Property of type Int64
        /// </summary>
        public Int64 Int64Property { get; set; }

        /// <summary>
        /// Property of type UInt64
        /// </summary>
        public UInt64 UInt64Property { get; set; }

        /// <summary>
        /// Property of type float
        /// </summary>
        public float FloatProperty { get; set; }

        /// <summary>
        /// Property of type double
        /// </summary>
        public double DoubleProperty { get; set; }

        /// <summary>
        /// Property of type string
        /// </summary>
        public string StringProperty { get; set; } = string.Empty;

        /// <summary>
        /// Property of type IntPtr
        /// </summary>
        public IntPtr IntPtrProperty { get; set; } = IntPtr.Zero;

        /// <summary>
        /// Property of type UIntPtr
        /// </summary>
        public UIntPtr UIntPtrProperty { get; set; } = UIntPtr.Zero;

        /// <summary>
        /// Property of type Object
        /// </summary>
        public Object ObjectProperty { get; set; } = new();

        /// <summary>
        /// Property of type DateTime
        /// </summary>
        public DateTime DateTimeProperty { get; set; }

        /// <summary>
        /// Property of type DateOnly
        /// </summary>
        public DateOnly DateOnlyProperty { get; set; }

        /// <summary>
        /// Property of type Guid
        /// </summary>
        public Guid GuidProperty { get; set; } = Guid.Empty;

        /// <summary>
        /// Property of type EnumDemo
        /// </summary>
        public EnumDemo EnumProperty { get; set; }
    }

    /// <summary>
    /// Class for the demo of this application.
    /// </summary>
    /// <remarks>
    /// It contains various array types.
    /// </remarks>
    [UseInFrontend(SubFolder = "Demo")]
    public class ArrayTypes
    {
        /// <summary>
        /// Boolean list.
        /// </summary>
        public List<bool> BooleanList { get; set; } = [];

        /// <summary>
        /// Single list.
        /// </summary>
        public IList<float> SingleList { get; set; } = [];

        /// <summary>
        /// Double list.
        /// </summary>
        public IReadOnlyList<double> DoubleList { get; set; } = [];

        /// <summary>
        /// Integer array.
        /// </summary>
        public int[] IntegerArray { get; set; } = [];
    }

    /// <summary>
    /// Class for the demo of this application.
    /// </summary>
    /// <remarks>
    /// This class contains private and protected member fields and properties.
    /// </remarks>
    [UseInFrontend(SubFolder = "Demo")]
    public class VariousMemberVisibilities
    {
        private int ThisShouldNotBeSerialized;

        private int ThisShouldAlsoNotBeSerialized { get; set; }

        /// <summary>
        /// This is a protected member field and should be serialized.
        /// </summary>
        protected int ThisShouldBeSerialized;

        /// <summary>
        /// This is a protected property and should be serialized.
        /// </summary>
        protected int ThisShouldAlsoBeSerialized { get; set; }
    }

    /// <summary>
    /// Class for the demo of this application.
    /// </summary>
    /// <remarks>
    /// This class contains properties with different case formatting.
    /// </remarks>
    [UseInFrontend(SubFolder = "Demo")]
    public class VariousMemberNameCasings : VariousMemberVisibilities
    {
        /// <summary>
        /// CamelCasing
        /// </summary>
        public int CamelCase { get; set; }

        /// <summary>
        /// pascalCasing
        /// </summary>
        public int pascalCase { get; set; }

        /// <summary>
        /// snakeCasing
        /// </summary>
        public int snake_case { get; set; }

        /// <summary>
        /// ALLUPPERCASE
        /// </summary>
        public int ALLUPPERCASE { get; set; }

        /// <summary>
        /// MULTIPLEUpperCaseAtBegin
        /// </summary>
        public int MULTIPLEUpperCaseAtBegin { get; set; }

        /// <summary>
        /// MultiplUPPERCASEIntheMiddle
        /// </summary>
        public int MultiplUPPERCASEIntheMiddle { get; set; }

        /// <summary>
        /// MultipleUpperCaseAtEND
        /// </summary>
        public int MultipleUpperCaseAtEND { get; set; }

        /// <summary>
        /// sOMEUpperCaseAfterTheFirstCharacter
        /// </summary>
        public int sOMEUpperCaseAfterTheFirstCharacter { get; set; }
    }
}