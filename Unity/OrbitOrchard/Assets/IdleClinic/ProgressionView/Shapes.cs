using UnityEngine;

namespace IdleClinic.ProgressionView
{
    /// <summary>Primitive-and-colour building blocks; the whole view is made from these so no art import is needed.</summary>
    internal static class Shapes
    {
        private static Shader shader;

        public static Material Paint(Color color)
        {
            if (shader == null) shader = Shader.Find("Standard");
            var material = new Material(shader) { color = color };
            material.SetFloat("_Glossiness", 0.1f);
            return material;
        }

        public static GameObject Make(PrimitiveType type, Transform parent, Vector3 localPosition, Vector3 scale, Color color, string name = null)
        {
            var go = GameObject.CreatePrimitive(type);
            if (name != null) go.name = name;
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = Paint(color);
            return go;
        }

        public static GameObject Box(Transform parent, Vector3 position, Vector3 scale, Color color, string name = null) =>
            Make(PrimitiveType.Cube, parent, position, scale, color, name);

        public static GameObject Empty(Transform parent, Vector3 localPosition, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            return go;
        }
    }
}
