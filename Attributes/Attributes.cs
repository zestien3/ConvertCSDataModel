using System;
using System.Collections.Generic;

namespace Zestien3.ConvertCSDataModel
{
    /// <summary>
    /// Enum to indicate the language(s) to which the classes should be converted.
    /// </summary>
    public enum Language
    {
        /// <summary>Translate the classes to TypeScript.</summary>
        TypeScript,

        /// <summary>Translate the classes to HTML.</summary>
        HTML
    }

    /// <summary>
    /// Enum to indicate the DialogType(s) to which the classes should be converted.
    /// </summary>
    public enum DialogType
    {
        /// <summary>Create a standard UI dialog.</summary>
        Standard,

        /// <summary>Create a compact UI dialog.</summary>
        Compact
    }

    /// <summary>
    /// This attribute will translate the class it is set on.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum, AllowMultiple = true)]
    public class UseInFrontendAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UseInFrontendAttribute"/> class.
        /// </summary>
        public UseInFrontendAttribute() { }

        /// <summary>
        /// The Language(s) to which the class should be translated.
        /// </summary>
        public Language Language { get; set; }

        /// <summary>
        /// The subfolder where the translated file will be generated.
        /// </summary>
        public string SubFolder { get; set; } = string.Empty;

        /// <summary>
        /// The properties which are hidden in the UI generation.
        /// </summary>
        public List<string> HiddenProperties { get; set; } = [];

        /// <summary>
        /// The type ofdialog that is created in the UI generation.
        /// </summary>
        public DialogType DialogType { get; set; } = DialogType.Standard;
    }

    /// <summary>
    /// This attribute defines the value of a parameter used when calling the base class constructor.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class FixedParameterValueAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FixedParameterValueAttribute"/> class.
        /// </summary>
        public FixedParameterValueAttribute() { }

        /// <summary>
        /// The name of the parameter for which we want to set a fixed value.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The value of the parameter for which we want to set a fixed value.
        /// </summary>
        public string Value { get; set; } = string.Empty;
    }
}