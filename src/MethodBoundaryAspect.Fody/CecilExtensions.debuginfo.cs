// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CecilExtensions.debuginfo.cs" company="Catel development team">
//   Copyright (c) 2008 - 2017 Catel development team. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


// ReSharper disable once CheckNamespace
namespace Catel.Fody
{
    using Mono.Cecil;
    using Mono.Cecil.Cil;
    using Mono.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// taken from https://github.com/Catel/Catel.Fody/blob/master/src/Catel.Fody/Extensions/CecilExtensions.debuginfo.cs#L23
    /// </summary>
    public static partial class CecilExtensions
    {
        public static void UpdateDebugInfo(this MethodDefinition method)
        {
            var debugInfo = method.DebugInformation;
            var instructions = method.Body.Instructions;
            var scope = debugInfo.Scope;

            if (scope == null || instructions.Count == 0)
            {
                return;
            }

            // Step 1: check if all variables are present
            foreach (var variable in method.Body.Variables)
            {
                if (!ContainsVariable(scope, variable))
                {
                    var variableDebugInfo = new VariableDebugInformation(variable, $"__var_{variable.Index}");
                    scope.Variables.Add(variableDebugInfo);
                }
            }

            // The sequence points reference the instructions themselves, so they follow them when the weaver inserts
            // instructions. Sequence points of instructions which are no longer part of the method are removed where
            // the weaver removes or moves the instructions, see MethodWeaver.ClearMethod and AsyncMethodWeaver.HandleBody.

            // Step 2: Remove any unused variables
            RemoveUnusedVariablesFromDebugInfo(scope);

            // Final step: update the scopes by setting the indices
            scope.Start = new InstructionOffset(instructions.First());
            scope.End = new InstructionOffset(instructions.Last());
        }

        private static bool ContainsVariable(this ScopeDebugInformation debugInfo, VariableDefinition variable)
        {
            // Note: just checking for index might not be sufficient
            var hasVariable = debugInfo.Variables.Any(x => x.Index == variable.Index);
            if (hasVariable)
            {
                return true;
            }

            // Important: check nested scopes
            for (var i = 0; i < debugInfo.Scopes.Count; i++)
            {
                if (ContainsVariable(debugInfo.Scopes[i], variable))
                {
                    return true;
                }
            }

            return false;
        }

        private static void RemoveUnusedVariablesFromDebugInfo(this ScopeDebugInformation debugInfo)
        {
            // Remove any variables that no longer have a valid index (such as -1)
            for (var i = 0; i < debugInfo.Variables.Count; i++)
            {
                var debugInfoVariable = debugInfo.Variables[i];
                if (debugInfoVariable.Index < 0)
                {
                    debugInfo.Variables.Remove(debugInfoVariable);
                    i--;
                }
            }

            // Important: nested scopes (for example, for async methods)
            for (var i = 0; i < debugInfo.Scopes.Count; i++)
            {
                RemoveUnusedVariablesFromDebugInfo(debugInfo.Scopes[i]);
            }
        }
    }
}
