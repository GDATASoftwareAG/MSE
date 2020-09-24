using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Controllers;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SampleExchangeApi.Console.Filters
{
    /// <summary>
    /// Path Parameter Validation Rules Filter
    /// </summary>
    public class GeneratePathParamsValidationFilter : IOperationFilter
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="operation">Operation</param>
        /// <param name="context">OperationFilterContext</param>
        public void Apply(Operation operation, OperationFilterContext context)
        {
            var pars = context.ApiDescription.ParameterDescriptions;

            foreach (var par in pars)
            {
                var swaggerParam = operation.Parameters.SingleOrDefault(p => p.Name == par.Name);

                var customAttributeData = ((ControllerParameterDescriptor) par.ParameterDescriptor).ParameterInfo
                    .CustomAttributes.ToList();

                var count = customAttributeData.Count;
                if (count > 0 && swaggerParam != null)
                {
                    // Required - [Required]
                    var requiredAttr =
                        customAttributeData.FirstOrDefault(p => p.AttributeType == typeof(RequiredAttribute));
                    if (requiredAttr != null)
                    {
                        swaggerParam.Required = true;
                    }

                    // Regex Pattern [RegularExpression]
                    var regexAttr =
                        customAttributeData.FirstOrDefault(p => p.AttributeType == typeof(RegularExpressionAttribute));
                    if (regexAttr != null)
                    {
                        var regex = (string) regexAttr.ConstructorArguments[0].Value;
                        if (swaggerParam is NonBodyParameter parameter)
                        {
                            parameter.Pattern = regex;
                        }
                    }

                    // String Length [StringLength]
                    int? minLength = null, maxLength = null;
                    var stringLengthAttr =
                        customAttributeData.FirstOrDefault(p => p.AttributeType == typeof(StringLengthAttribute));
                    if (stringLengthAttr != null)
                    {
                        if (stringLengthAttr.NamedArguments != null && stringLengthAttr.NamedArguments.Count == 1)
                        {
                            minLength = (int) stringLengthAttr.NamedArguments
                                .Single(p => p.MemberName == "MinimumLength").TypedValue.Value;
                        }

                        maxLength = (int) stringLengthAttr.ConstructorArguments[0].Value;
                    }

                    var minLengthAttr =
                        customAttributeData.FirstOrDefault(p => p.AttributeType == typeof(MinLengthAttribute));
                    if (minLengthAttr != null)
                    {
                        minLength = (int) minLengthAttr.ConstructorArguments[0].Value;
                    }

                    var maxLengthAttr =
                        customAttributeData.FirstOrDefault(p => p.AttributeType == typeof(MaxLengthAttribute));
                    if (maxLengthAttr != null)
                    {
                        maxLength = (int) maxLengthAttr.ConstructorArguments[0].Value;
                    }

                    if (swaggerParam is NonBodyParameter bodyParameter)
                    {
                        bodyParameter.MinLength = minLength;
                        bodyParameter.MaxLength = maxLength;
                    }

                    // Range [Range]
                    var rangeAttr = customAttributeData.FirstOrDefault(p => p.AttributeType == typeof(RangeAttribute));
                    if (rangeAttr != null)
                    {
                        var rangeMin = (int) rangeAttr.ConstructorArguments[0].Value;
                        var rangeMax = (int) rangeAttr.ConstructorArguments[1].Value;

                        if (swaggerParam is NonBodyParameter parameter)
                        {
                            parameter.Minimum = rangeMin;
                            parameter.Maximum = rangeMax;
                        }
                    }
                }
            }
        }
    }
}