using System;
using System.Collections.Generic;
using UnityEngine;

namespace FinalFreedom
{
    // Pool có giới hạn cứng: tránh tăng bộ nhớ khi chơi lâu; trả đối tượng không Destroy.
    public sealed class ObjectPooler
    {
        readonly List<GameObject> objects = new List<GameObject>();
        readonly Func<GameObject> create;
        readonly Transform root;
        readonly int capacity;
        public IReadOnlyList<GameObject> Items => objects;
        public ObjectPooler(Transform root, Func<GameObject> create, int warm, int capacity)
        {
            this.root = root; this.create = create; this.capacity = capacity;
            for (int i = 0; i < warm; i++) Add();
        }
        GameObject Add()
        {
            var go = create();
            go.transform.SetParent(root, false);
            go.SetActive(false);
            objects.Add(go);
            return go;
        }
        // Rent trả object chưa active: bên gọi cấu hình lại HP/vận tốc trước khi bật.
        public GameObject Rent()
        {
            foreach (var go in objects) if (!go.activeSelf) return go;
            return objects.Count < capacity ? Add() : null;
        }
        public void ReturnAll()
        {
            foreach (var go in objects) go.SetActive(false);
        }
    }
}
