using System.Collections.Generic;

namespace TriLibCore.Dae.Schema
{
    public static class COLLADAHelper
    {

        public static InputLocal GetItemWithSematic(InputLocal[] inputs, string semantic)
        {
            if (inputs != null)
            {
                foreach (var input in inputs)
                {
                    if (input.semantic == semantic)
                    {
                        return input;
                    }
                }
            }

            return null;
        }

        public static T GetItemWithType<T>(this object[] items) where T : class
        {
            if (items != null)
            {
                foreach (var item in items)
                {
                    if (item is T tItem)
                    {
                        return tItem;
                    }
                }
            }

            return null;
        }

        
        public static IList<T> GetItemsWithType<T>(this object[] items) where T : class
        {
            List<T> tItems = null;
            if (items != null)
            {
                foreach (var item in items)
                {
                    if (item is T tItem)
                    {
                        if (tItems == null)
                        {
                            tItems = new List<T>();
                        }

                        tItems.Add(tItem);
                    }
                }
            }

            return tItems;
        }

        public static T GetItemWithIdNoHash<T>(this IList<object> items, string id) where T : class
        {
            if (items != null)
            {
                foreach (var item in items)
                {
                    if (item is common_newparam_type commonNewparamType && commonNewparamType.sid == id)
                    {
                        return (T)commonNewparamType.Item;
                    }
                }
            }

            return null;
        }

        public static T GetItemWithId<T>(this IList<T> items, string id) where T : class
        {
            if (items != null)
            {
                foreach (var item in items)
                {
                    switch (item)
                    {
                        case source source when $"#{source.id}" == id:
                            return item;
                        case geometry geometry when $"#{geometry.id}" == id:
                            return item;
                        case visual_scene visualScene when $"#{visualScene.id}" == id:
                            return item;
                        case effect effect when $"#{effect.id}" == id:
                            return item;
                        case controller controller when $"#{controller.id}" == id:
                            return item;
                        case sampler sampler when $"#{sampler.id}" == id:
                            return item;
                        case node node when $"#{node.id}" == id:
                            return item;
                    }
                }

            }

            return null;
        }

        public static bool HasInputWithSemantic<T>(this T[] items, string semantic) where T : class
        {
            if (items != null)
            {
                foreach (var item in items)
                {
                    switch (item)
                    {
                        case InputLocal inputLocal when inputLocal.semantic == semantic:
                        case InputLocalOffset inputLocalOffset when inputLocalOffset.semantic == semantic:
                            return true;
                    }
                }
            }

            return false;
        }

        public static T GetInputWithSemantic<T>(this T[] items, string semantic) where T : class
        {
            if (items != null)
            {
                foreach (var item in items)
                {
                    switch (item)
                    {
                        case InputLocal inputLocal when inputLocal.semantic == semantic:
                            return item;
                        case InputLocalOffset inputLocalOffset when inputLocalOffset.semantic == semantic:
                            return item;
                    }
                }
            }

            return null;
        }
    }
}
