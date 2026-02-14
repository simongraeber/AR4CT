namespace TriLibCore.Dae.Schema
{
    public static class ItemSelector
    {
        public static bool HasItems(object[] items)
        {
            return items is effectFx_profile_abstractProfile_CG[] ||
                   items is effectFx_profile_abstractProfile_COMMON[] ||
                   items is effectFx_profile_abstractProfile_GLES[] ||
                   items is effectFx_profile_abstractProfile_GLSL[];
        }

        public static object[] GetItems(object item)
        {
            switch (item)
            {
                case effectFx_profile_abstractProfile_CG a:
                    return a.Items;
                case effectFx_profile_abstractProfile_COMMON b:
                    return b.Items;
                case effectFx_profile_abstractProfile_GLES c:
                    return c.Items;
                case effectFx_profile_abstractProfile_GLSL d:
                    return d.Items;
                default:
                    return null;
            }
        }

        public static object GetTechniqueItem(object item)
        {
            switch (item)
            {
                case effectFx_profile_abstractProfile_CG a:
                    return a.technique?[0].Items?[0];
                case effectFx_profile_abstractProfile_COMMON b:
                    return b.technique.Item;
                case effectFx_profile_abstractProfile_GLES c:
                    return c.technique?[0].Items?[0];
                case effectFx_profile_abstractProfile_GLSL d:
                    return d.technique?[0].Items?[0];
                default:
                    return null;
            }
        }

        public static extra[] GetTechniqueExtra(object item)
        {
            switch (item)
            {
                case effectFx_profile_abstractProfile_CG a:
                    return a.technique?[0].extra;
                case effectFx_profile_abstractProfile_COMMON b:
                    return b.technique.extra;
                case effectFx_profile_abstractProfile_GLES c:
                    return c.technique?[0].extra;
                case effectFx_profile_abstractProfile_GLSL d:
                    return d.technique?[0].extra;
                default:
                    return null;
            }
        }
    }
}