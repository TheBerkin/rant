﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Rant.Engine.Constructs;
using Rant.Engine.Formatters;
using Rant.Engine.Metadata;
using Rant.Engine.Syntax.Expressions;
using Rant.Engine.ObjectModel;
using Rant.Engine.Syntax;
using Rant.Vocabulary;

namespace Rant.Engine
{
    internal class RichardFunctions
    {
        private static bool _loaded = false;
        private static Dictionary<Type, Dictionary<string, RantFunctionInfo>> _properties;
        private static Dictionary<string, REAObject> _globalObjects;

        static RichardFunctions()
        {
            Load();
        }

        public static void Load()
        {
            if (_loaded) return;

            _properties = new Dictionary<Type, Dictionary<string, RantFunctionInfo>>();
            _globalObjects = new Dictionary<string, REAObject>();

            foreach (var method in typeof(RichardFunctions).GetMethods(BindingFlags.Static | BindingFlags.NonPublic))
            {
                if (!method.IsStatic) continue;
                var attr = method.GetCustomAttributes().OfType<RichardProperty>().FirstOrDefault();
                if (attr == null)
                {
                    var objectAttr = method.GetCustomAttributes().OfType<RichardGlobalObject>().FirstOrDefault();
                    if (objectAttr == null) continue;
                    var name = String.IsNullOrEmpty(objectAttr.Property) ? method.Name.ToLower() : objectAttr.Property;
                    var descAttr = method.GetCustomAttributes().OfType<RantDescriptionAttribute>().FirstOrDefault();
                    var info = new RantFunctionInfo(name, descAttr?.Description ?? String.Empty, method);
                    info.TreatAsRichardFunction = objectAttr.IsFunction;
                    if (Util.ValidateName(name)) RegisterGlobalObject(objectAttr.Name, objectAttr.Property, info);
                }
                else
                {
                    var name = String.IsNullOrEmpty(attr.Name) ? method.Name.ToLower() : attr.Name;
                    var descAttr = method.GetCustomAttributes().OfType<RantDescriptionAttribute>().FirstOrDefault();
                    var info = new RantFunctionInfo(name, descAttr?.Description ?? String.Empty, method);
                    info.TreatAsRichardFunction = attr.IsFunction;
                    if (Util.ValidateName(name)) RegisterProperty(attr.ObjectType, info);
                }
            }

            _loaded = true;
        }

        private static void RegisterProperty(Type objectType, RantFunctionInfo info)
        {
            var dictionary = _properties.ContainsKey(objectType) ?
                _properties[objectType] :
                new Dictionary<string, RantFunctionInfo>();
            dictionary.Add(info.Name, info);
            _properties[objectType] = dictionary;
        }

        public static bool HasProperty(Type objectType, string name) =>
            _properties.ContainsKey(objectType) ? _properties[objectType].ContainsKey(name) : false;

        public static RantFunctionInfo GetProperty(Type objectType, string name)
        {
            if (!HasProperty(objectType, name))
                return null;
            return _properties[objectType][name];
        }

        private static void RegisterGlobalObject(string name, string property, RantFunctionInfo info)
        {
            if (!_globalObjects.ContainsKey(name))
                _globalObjects[name] = new REAObject(null);
            if (info.TreatAsRichardFunction)
                _globalObjects[name].Values[property] = new REANativeFunction(null, info.ParamCount - 1, info);
            else
                _globalObjects[name].Values[property] = new REAFunctionInfo(null, info);
        }

        public static bool HasObject(string name) => _globalObjects.ContainsKey(name);

        public static REAObject GetObject(string name)
        {
            if (!HasObject(name))
                return null;
            return _globalObjects[name];
        }

        // String properties
        [RichardProperty("length", typeof(string), Returns = "number", IsFunction = false)]
        [RantDescription("Returns the length of this string.")]
        private static IEnumerator<RantAction> StringLength(Sandbox sb, RantObject that)
        {
            yield return new REANumber((that.Value as string).Length, null);
        }

        // List properties
        [RichardProperty("length", typeof(REAList), Returns = "number", IsFunction = false)]
        [RantDescription("Returns the length of this list.")]
        private static IEnumerator<RantAction> ListLength(Sandbox sb, RantObject that)
        {
            yield return that.Value as REAList;
            var list = sb.ScriptObjectStack.Pop();
            yield return new REANumber((list as REAList).Items.Count, null);
        }

        [RichardProperty("last", typeof(REAList), Returns = "any", IsFunction = false)]
        [RantDescription("Returns the last item of this list.")]
        private static IEnumerator<RantAction> ListLast(Sandbox sb, RantObject that)
        {
            yield return that.Value as REAList;
            var list = sb.ScriptObjectStack.Pop();
            yield return (list as REAList).Items.Last();
        }

        [RichardProperty("push", typeof(REAList))]
        [RantDescription("Pushes the given item onto the end of this list.")]
        [RichardPropertyArgument("item", "any", Description = "The item to push onto the list.")]
        private static IEnumerator<RantAction> ListPush(Sandbox sb, RantObject that, RantObject obj)
        {
            (that.Value as REAList).Items.Add(new READummy(null, obj.Value));
            yield break;
        }

        [RichardProperty("pop", typeof(REAList), Returns = "any")]
        [RantDescription("Pops the last item from this list.")]
        private static IEnumerator<RantAction> ListPop(Sandbox sb, RantObject that)
        {
            var last = (that.Value as REAList).Items.Last();
            (that.Value as REAList).Items.RemoveAt((that.Value as REAList).Items.Count - 1);
            yield return last;
        }

        [RichardProperty("pushFront", typeof(REAList))]
        [RantDescription("Pushes the given item onto the front of this list.")]
        [RichardPropertyArgument("item", "any", Description = "The item to push onto the list.")]
        private static IEnumerator<RantAction> ListPushFront(Sandbox sb, RantObject that, RantObject obj)
        {
            (that.Value as REAList).Items.Insert(0, new READummy(null, obj.Value));
            yield break;
        }

        [RichardProperty("popFront", typeof(REAList), Returns = "any")]
        [RantDescription("Pops the first item from this list.")]
        private static IEnumerator<RantAction> ListPopFront(Sandbox sb, RantObject that)
        {
            var first = (that.Value as REAList).Items.First();
            (that.Value as REAList).Items.RemoveAt(0);
            yield return first;
        }

        [RichardProperty("copy", typeof(REAList), Returns = "list")]
        [RantDescription("Returns a new list with identical items.")]
        private static IEnumerator<RantAction> ListCopy(Sandbox sb, RantObject that)
        {
            var newList = (that.Value as REAList).Items.ToList();
            yield return new REAList((that.Value as REAList).Range, newList, false);
        }

        [RichardProperty("fill", typeof(REAList))]
        [RantDescription("Fills every item of the list with a specified value.")]
        [RichardPropertyArgument("value", "any", Description = "The value to fill the list with.")]
        private static IEnumerator<RantAction> ListFill(Sandbox sb, RantObject that, RantObject obj)
        {
            var fillValue = obj.Value;
            (that.Value as REAList).Items =
                (that.Value as REAList).Items.Select(
                    x => (RantExpressionAction)new READummy(null, fillValue)
                ).ToList();
            yield break;
        }

        [RichardProperty("reverse", typeof(REAList))]
        [RantDescription("Reverses this list in place.")]
        private static IEnumerator<RantAction> ListReverse(Sandbox sb, RantObject that)
        {
            (that.Value as REAList).Items.Reverse();
            yield break;
        }

        [RichardProperty("join", typeof(REAList), Returns = "string")]
        [RantDescription("Returns the list items joined together by a specified string.")]
        [RichardPropertyArgument("seperator", "string", Description = "The string to seperate each item of the list.")]
        private static IEnumerator<RantAction> ListJoin(Sandbox sb, RantObject that, RantObject obj)
        {
            sb.ScriptObjectStack.Push(string.Join(obj.ToString(), (that.Value as REAList).Items));
            yield break;
        }

        [RichardProperty("insert", typeof(REAList))]
        [RantDescription("Inserts an item into a specified position in this list.")]
        [RichardPropertyArgument("item", "any", Description = "The item to insert into this list.")]
        [RichardPropertyArgument("position", "number", Description = "The position in this list to insert the item into.")]
        private static IEnumerator<RantAction> ListInsert(Sandbox sb, RantObject that, RantObject item, RantObject pos)
        {
            var obj = item.Value;
            var position = pos.Value;
            if (!(position is double))
                throw new RantRuntimeException(sb.Pattern, null, "Position must be a number.");
            (that.Value as REAList).Items.Insert(Convert.ToInt32(position), new READummy(null, obj));
            yield break;
        }

        [RichardProperty("remove", typeof(REAList))]
        [RantDescription("Removes an item from a specified position in this list.")]
        [RichardPropertyArgument("position", "number", Description = "The position in this list to remove the item from.")]
        private static IEnumerator<RantAction> ListRemove(Sandbox sb, RantObject that, RantObject pos)
        {
            var position = pos.Value;
            if (!(position is double))
                throw new RantRuntimeException(sb.Pattern, null, "Position must be a number.");
            (that.Value as REAList).Items.RemoveAt(Convert.ToInt32(position));
            yield break;
        }


        /* 
        Global Objects
        */

        [RichardGlobalObject("Convert", "toString", Returns = "string")]
        [RichardPropertyArgument("object", "any", Description = "The object to convert to a string.")]
        [RantDescription("Converts the specified object to a string.")]
        private static IEnumerator<RantExpressionAction> ConvertToString(Sandbox sb, RantObject that, RantObject obj)
        {
            sb.ScriptObjectStack.Push(obj.ToString());
            yield break;
        }


        [RichardGlobalObject("Output", "print")]
        [RichardPropertyArgument("object", "any", Description = "The object to print.")]
        [RantDescription("Prints the provided object, cast to a string.")]
        private static IEnumerator<RantExpressionAction> OutputPrint(Sandbox sb, RantObject that, RantObject obj)
        {
            sb.Print(obj.ToString());
            yield break;
        }

        [RichardGlobalObject("Type", "get", Returns = "string")]
        [RichardPropertyArgument("object", "any", Description = "The object whose type will be returned.")]
        [RantDescription("Checks the type of a provided object and returns it.")]
        private static IEnumerator<RantExpressionAction> TypeGet(Sandbox sb, RantObject that, RantObject obj)
        {
            string type = Util.ScriptingObjectType(obj);
            sb.ScriptObjectStack.Push(type);
            yield break;
        }

        [RichardGlobalObject("Math", "E", Returns = "number", IsFunction = false)]
        [RantDescription("Returns the value of the mathematical constant E.")]
        private static IEnumerator<RantExpressionAction> MathE(Sandbox sb)
        {
            sb.ScriptObjectStack.Push(Math.E);
            yield break;
        }

        [RichardGlobalObject("Math", "PI", Returns = "number", IsFunction = false)]
        [RantDescription("Returns the value of the mathematical constant Pi.")]
        private static IEnumerator<RantExpressionAction> MathPI(Sandbox sb)
        {
            sb.ScriptObjectStack.Push(Math.PI);
            yield break;
        }
        [RichardGlobalObject("Math", "acos", Returns = "number")]
        [RichardPropertyArgument("n", "number", Description = "The number that will be used in this operation.")]
        [RantDescription("Returns acos(n).")]
        private static IEnumerator<RantExpressionAction> MathAcos(Sandbox sb, RantObject that, RantObject obj)
        {
            var num = obj.Value;
            if (!(num is double))
                throw new RantRuntimeException(sb.Pattern, null, "N must be a number.");
            sb.ScriptObjectStack.Push(Math.Acos((double)num));
            yield break;
        }

        [RichardGlobalObject("Math", "asin", Returns = "number")]
        [RichardPropertyArgument("n", "number", Description = "The number that will be used in this operation.")]
        [RantDescription("Returns asin(n).")]
        private static IEnumerator<RantExpressionAction> MathAsin(Sandbox sb, RantObject that, RantObject obj)
        {
            var num = obj.Value;
            if (!(num is double))
                throw new RantRuntimeException(sb.Pattern, null, "N must be a number.");
            sb.ScriptObjectStack.Push(Math.Asin((double)num));
            yield break;
        }

        [RichardGlobalObject("Math", "atan", Returns = "number")]
        [RichardPropertyArgument("n", "number", Description = "The number that will be used in this operation.")]
        [RantDescription("Returns atan(n).")]
        private static IEnumerator<RantExpressionAction> MathAtan(Sandbox sb, RantObject that, RantObject obj)
        {
            var num = obj.Value;
            if (!(num is double))
                throw new RantRuntimeException(sb.Pattern, null, "N must be a number.");
            sb.ScriptObjectStack.Push(Math.Atan((double)num));
            yield break;
        }
        [RichardGlobalObject("Math", "atan2", Returns = "number")]
        [RichardPropertyArgument("y", "number", Description = "The Y value of the atan2 operation.")]
        [RichardPropertyArgument("x", "number", Description = "The X value of the atan2 operation.")]
        [RantDescription("Returns atan2(n).")]
        private static IEnumerator<RantExpressionAction> MathAtan2(Sandbox sb, RantObject that, RantObject arg1, RantObject arg2)
        {
            var y = arg1.Value;
            if (!(y is double))
                throw new RantRuntimeException(sb.Pattern, null, "Y must be a number.");
            var x = arg2.Value;
            if (!(x is double))
                throw new RantRuntimeException(sb.Pattern, null, "X must be a number.");
            sb.ScriptObjectStack.Push(Math.Atan2((double)y, (double)x));
            yield break;
        }

        [RichardGlobalObject("Math", "ceil", Returns = "number")]
        [RichardPropertyArgument("n", "number", Description = "The number that will be used in this operation.")]
        [RantDescription("Returns ceil(n).")]
        private static IEnumerator<RantExpressionAction> MathCeil(Sandbox sb, RantObject that, RantObject obj)
        {
            var num = obj.Value;
            if (!(num is double))
                throw new RantRuntimeException(sb.Pattern, null, "N must be a number.");
            sb.ScriptObjectStack.Push(Math.Ceiling((double)num));
            yield break;
        }

        [RichardGlobalObject("Math", "cos", Returns = "number")]
        [RichardPropertyArgument("n", "number", Description = "The number that will be used in this operation.")]
        [RantDescription("Returns cos(n).")]
        private static IEnumerator<RantExpressionAction> MathCos(Sandbox sb, RantObject that, RantObject obj)
        {
            var num = obj.Value;
            if (!(num is double))
                throw new RantRuntimeException(sb.Pattern, null, "N must be a number.");
            sb.ScriptObjectStack.Push(Math.Cos((double)num));
            yield break;
        }

        [RichardGlobalObject("Math", "cosh", Returns = "number")]
        [RichardPropertyArgument("n", "number", Description = "The number that will be used in this operation.")]
        [RantDescription("Returns cosh(n).")]
        private static IEnumerator<RantExpressionAction> MathCosh(Sandbox sb, RantObject that, RantObject obj)
        {
            var num = obj.Value;
            if (!(num is double))
                throw new RantRuntimeException(sb.Pattern, null, "N must be a number.");
            sb.ScriptObjectStack.Push(Math.Cosh((double)num));
            yield break;
        }

        [RichardGlobalObject("Math", "exp", Returns = "number")]
        [RichardPropertyArgument("n", "number", Description = "The number that will be used in this operation.")]
        [RantDescription("Returns exp(n).")]
        private static IEnumerator<RantExpressionAction> MathExp(Sandbox sb, RantObject that, RantObject obj)
        {
            var num = obj.Value;
            if (!(num is double))
                throw new RantRuntimeException(sb.Pattern, null, "N must be a number.");
            sb.ScriptObjectStack.Push(Math.Exp((double)num));
            yield break;
        }

        [RichardGlobalObject("Math", "floor", Returns = "number")]
        [RichardPropertyArgument("n", "number", Description = "The number that will be used in this operation.")]
        [RantDescription("Returns floor(n).")]
        private static IEnumerator<RantExpressionAction> MathFloor(Sandbox sb, RantObject that, RantObject obj)
        {
            var num = obj.Value;
            if (!(num is double))
                throw new RantRuntimeException(sb.Pattern, null, "N must be a number.");
            sb.ScriptObjectStack.Push(Math.Floor((double)num));
            yield break;
        }

        [RichardGlobalObject("Math", "log", Returns = "number")]
        [RichardPropertyArgument("n", "number", Description = "The number that will be used in this operation.")]
        [RantDescription("Returns log(n).")]
        private static IEnumerator<RantExpressionAction> MathLog(Sandbox sb, RantObject that, RantObject obj)
        {
            var num = obj.Value;
            if (!(num is double))
                throw new RantRuntimeException(sb.Pattern, null, "N must be a number.");
            sb.ScriptObjectStack.Push(Math.Log((double)num));
            yield break;
        }

        [RichardGlobalObject("Math", "log10", Returns = "number")]
        [RichardPropertyArgument("n", "number", Description = "The number that will be used in this operation.")]
        [RantDescription("Returns log10(n).")]
        private static IEnumerator<RantExpressionAction> MathLog10(Sandbox sb, RantObject that, RantObject obj)
        {
            var num = obj.Value;
            if (!(num is double))
                throw new RantRuntimeException(sb.Pattern, null, "N must be a number.");
            sb.ScriptObjectStack.Push(Math.Log10((double)num));
            yield break;
        }

        [RichardGlobalObject("Math", "round", Returns = "number")]
        [RichardPropertyArgument("n", "number", Description = "The number that will be used in this operation.")]
        [RantDescription("Returns round(n).")]
        private static IEnumerator<RantExpressionAction> MathRound(Sandbox sb, RantObject that, RantObject obj)
        {
            var num = obj.Value;
            if (!(num is double))
                throw new RantRuntimeException(sb.Pattern, null, "N must be a number.");
            sb.ScriptObjectStack.Push(Math.Round((double)num));
            yield break;
        }

        [RichardGlobalObject("Math", "sin", Returns = "number")]
        [RichardPropertyArgument("n", "number", Description = "The number that will be used in this operation.")]
        [RantDescription("Returns sin(n).")]
        private static IEnumerator<RantExpressionAction> MathSin(Sandbox sb, RantObject that, RantObject obj)
        {
            var num = obj.Value;
            if (!(num is double))
                throw new RantRuntimeException(sb.Pattern, null, "N must be a number.");
            sb.ScriptObjectStack.Push(Math.Sin((double)num));
            yield break;
        }

        [RichardGlobalObject("Math", "sinh", Returns = "number")]
        [RichardPropertyArgument("n", "number", Description = "The number that will be used in this operation.")]
        [RantDescription("Returns sinh(n).")]
        private static IEnumerator<RantExpressionAction> MathSinh(Sandbox sb, RantObject that, RantObject obj)
        {
            var num = obj.Value;
            if (!(num is double))
                throw new RantRuntimeException(sb.Pattern, null, "N must be a number.");
            sb.ScriptObjectStack.Push(Math.Sinh((double)num));
            yield break;
        }

        [RichardGlobalObject("Math", "sqrt", Returns = "number")]
        [RichardPropertyArgument("n", "number", Description = "The number that will be used in this operation.")]
        [RantDescription("Returns sqrt(n).")]
        private static IEnumerator<RantExpressionAction> MathSqrt(Sandbox sb, RantObject that, RantObject obj)
        {
            var num = obj.Value;
            if (!(num is double))
                throw new RantRuntimeException(sb.Pattern, null, "N must be a number.");
            sb.ScriptObjectStack.Push(Math.Sqrt((double)num));
            yield break;
        }

        [RichardGlobalObject("Math", "tan", Returns = "number")]
        [RichardPropertyArgument("n", "number", Description = "The number that will be used in this operation.")]
        [RantDescription("Returns tan(n).")]
        private static IEnumerator<RantExpressionAction> MathTan(Sandbox sb, RantObject that, RantObject obj)
        {
            var num = obj.Value;
            if (!(num is double))
                throw new RantRuntimeException(sb.Pattern, null, "N must be a number.");
            sb.ScriptObjectStack.Push(Math.Tan((double)num));
            yield break;
        }

        [RichardGlobalObject("Math", "tanh", Returns = "number")]
        [RichardPropertyArgument("n", "number", Description = "The number that will be used in this operation.")]
        [RantDescription("Returns tanh(n).")]
        private static IEnumerator<RantExpressionAction> MathTanh(Sandbox sb, RantObject that, RantObject obj)
        {
            var num = obj.Value;
            if (!(num is double))
                throw new RantRuntimeException(sb.Pattern, null, "N must be a number.");
            sb.ScriptObjectStack.Push(Math.Tanh((double)num));
            yield break;
        }

        [RichardGlobalObject("Math", "truncate", Returns = "number")]
        [RichardPropertyArgument("n", "number", Description = "The number that will be used in this operation.")]
        [RantDescription("Returns truncate(n).")]
        private static IEnumerator<RantExpressionAction> MathTruncate(Sandbox sb, RantObject that, RantObject obj)
        {
            var num = obj.Value;
            if (!(num is double))
                throw new RantRuntimeException(sb.Pattern, null, "N must be a number.");
            sb.ScriptObjectStack.Push(Math.Truncate((double)num));
            yield break;
        }
    }

    internal class RichardProperty : Attribute
    {
        public string Name;
        public Type ObjectType;
        public string Returns;
        public bool IsFunction = true;

        public RichardProperty(string name, Type objectType)
        {
            Name = name;
            ObjectType = objectType;
        }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    internal class RichardPropertyArgument : Attribute
    {
        public string Name;
        public string Description;
        public string Type;

        public RichardPropertyArgument(string name, string type)
        {
            Name = name;
            Type = type;
        }
    }

    internal class RichardGlobalObject : Attribute
    {
        public string Name;
        public string Property;
        public string Returns;
        public bool IsFunction = true;

        public RichardGlobalObject(string name, string property)
        {
            Name = name;
            Property = property;
        }
    }
}