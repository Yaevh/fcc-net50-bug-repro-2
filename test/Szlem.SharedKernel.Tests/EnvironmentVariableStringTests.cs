using System;
using System.Collections.Generic;
using System.Text;
using Szlem.SharedKernel;
using Xunit;

namespace Szlem.SharedKernel.Tests
{
    public class EnvironmentVariableStringTests
    {
        [Theory(DisplayName = "input string without envvars produces the same string")]
        [InlineData("ala ma kota")]
        [InlineData("C:/Windows")]
        [InlineData("")]
        public void String_without_envvars_produces_the_same_string(string input)
        {
            var sut = new EnvironmentVariableString(input);
            Assert.Equal(input, sut);
        }

        [Theory(DisplayName = "input string with envvars produces string with envvars replaced by their values")]
        [InlineData("%HOMEDIR%")]
        [InlineData("%windir%")]
        [InlineData("Data Source=%APPDATA%\\Szlem\\data\\prod\\db.sqlite")]
        [InlineData("Data Source=%APPDATA%/Szlem/data/prod/db.sqlite")]
        public void String_with_envvars_produces_string_with_replaced_envvars(string input)
        {
            var sut = new EnvironmentVariableString(input);
            Assert.Equal(Environment.ExpandEnvironmentVariables(input), sut);
        }

        [Theory(DisplayName = "RawValue property points to original string")]
        [InlineData("%HOMEDIR%")]
        [InlineData("%windir%")]
        [InlineData("Data Source=%APPDATA%\\Szlem\\data\\prod\\db.sqlite")]
        [InlineData("Data Source=%APPDATA%/Szlem/data/prod/db.sqlite")]
        public void RawValue_property_points_to_original_string(string input)
        {
            var sut = new EnvironmentVariableString(input);
            Assert.Equal(input, sut.RawValue);
        }

        [Theory(DisplayName = "ExpandedValue property points to expanded string")]
        [InlineData("%HOMEDIR%")]
        [InlineData("%windir%")]
        [InlineData("Data Source=%APPDATA%\\Szlem\\data\\prod\\db.sqlite")]
        [InlineData("Data Source=%APPDATA%/Szlem/data/prod/db.sqlite")]
        public void ExpandedValue_property_points_to_original_string(string input)
        {
            var sut = new EnvironmentVariableString(input);
            Assert.Equal(Environment.ExpandEnvironmentVariables(input), sut.ExpandedValue);
        }

        [Fact]
        public void SystemComponentModelTypeConverter_serializes_RawValue()
        {
            var serialized = Convert.ToString(new EnvironmentVariableString("%HOMEDIR%"));
            Assert.Equal(new EnvironmentVariableString("%HOMEDIR%").RawValue, serialized);
            var deserialized =System.ComponentModel.TypeDescriptor.GetConverter(typeof(EnvironmentVariableString)).ConvertFromString(serialized);
            Assert.Equal(new EnvironmentVariableString("%HOMEDIR%"), deserialized);
        }

        [Fact]
        public void NewtonsoftJson_serializer_serializes_RawValue()
        {
            var serialized = Newtonsoft.Json.JsonConvert.SerializeObject(new EnvironmentVariableString("%HOMEDIR%"));
            Assert.Equal("\"" + new EnvironmentVariableString("%HOMEDIR%").RawValue + "\"", serialized);
            var deserialized = Newtonsoft.Json.JsonConvert.DeserializeObject<EnvironmentVariableString>(serialized);
            Assert.Equal(new EnvironmentVariableString("%HOMEDIR%"), deserialized);
        }

        [Fact]
        public void SystemTextJson_serializer_serializes_RawValue()
        {
            var serialized = System.Text.Json.JsonSerializer.Serialize(new EnvironmentVariableString("%HOMEDIR%"));
            Assert.Equal("\"" + new EnvironmentVariableString("%HOMEDIR%").RawValue + "\"", serialized);
            var deserialized = System.Text.Json.JsonSerializer.Deserialize<EnvironmentVariableString>(serialized);
            Assert.Equal(new EnvironmentVariableString("%HOMEDIR%"), deserialized);
        }
    }
}
