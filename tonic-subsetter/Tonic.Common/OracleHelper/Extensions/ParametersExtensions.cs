using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Tonic.Common.OracleHelper.ErrorCodes;
using Tonic.Common.OracleHelper.Models;
using Tonic.Common.OracleHelper.Models.DataPump;

namespace Tonic.Common.OracleHelper.Extensions
{
    public static class DataPumpParametersExtensions
    {
        /// <summary>
        /// Optional validation of parameter conflicts, invalid values, etc.
        /// </summary>
        /// <remarks>This is a working list for Tonic use-cases and far from a complete test for validity</remarks>
        /// <returns>true if valid, otherwise false with errors out</returns>
        public static bool Validate(this DataPumpParameters parameters, out IEnumerable<string> validationErrors)
        {
            var errors = new List<string>();
            validationErrors = errors;

            if (!string.IsNullOrEmpty(parameters.ParametersFile))
            {
                errors.Add($"{parameters.GetParameterAttribute(nameof(parameters.ParametersFile))?.Name} should only be specified in a command-line");
            }

            if (parameters.Schemas?.Length > 0 && parameters.Tables?.Length > 0)
            {
                errors.Add($"UDI-00010: multiple job modes requested, schema ({parameters.Schemas.Length}) and tables ({parameters.Tables.Length}).");
            }

            if (parameters.Include?.Length > 0 && parameters.Exclude?.Length > 0)
            {
                errors.Add($"Include and Exclude are mutually exclusive options.");
            }

            if (parameters.Tables?.Length > 0 &&
                (parameters.Include != null && parameters.Include.ContainsAny(OracleObjectType.Procedure, OracleObjectType.Function) ||
                 parameters.Exclude != null && parameters.Exclude.ContainsAny(OracleObjectType.Procedure, OracleObjectType.Function)))
            {
                errors.Add(
                $"{OracleErrorCodes.DataPump.ObjectPathNotSupportedForTableJobs}: Object path \"FUNCTION\", \"PROCEDURE\" are not supported for TABLE jobs."
                    );
            }

            if (parameters is DataPumpImportParameters dataPumpImportParameters)
            {
                Validate(dataPumpImportParameters, ref errors);
            }

            return errors.Count == 0;
        }

        private static void Validate(this DataPumpImportParameters parameters, ref List<string> validationErrors)
        {
            if (parameters.Transform?.Length > 0)
            {
                for (int i = 0; i < parameters.Transform.Length; i++)
                {
                    var transform = parameters.Transform[i];
                    TransformFactory.Validate(transform, ref validationErrors);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ContainsAny(this OracleObjectType[]? objectTypes, params OracleObjectType[] values)
        {
            if (objectTypes != null)
            {
                for (int i = 0; i < objectTypes.Length; i++)
                {
                    var objectType = objectTypes[i];

                    for (int j = 0; j < values.Length; j++)
                    {
                        var value = values[j];

                        if (objectType == value)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        internal static T WithCredentialsRemoved<T>(this T toolParameters)
            where T : ParametersBase
        {
            switch (toolParameters)
            {
                case DataPumpParameters dataPumpParameters:
                    dataPumpParameters.UserId = Constants.RedactedValue;
                    dataPumpParameters.EncryptionPassword = Constants.RedactedValue;
                    break;
                case SqlLoaderParameters sqlLoaderParameters:
                    sqlLoaderParameters.UserId = Constants.RedactedValue;
                    break;
            }

            return toolParameters;
        }
    }
}