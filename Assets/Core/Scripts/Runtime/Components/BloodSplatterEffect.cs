using UnityEngine;

namespace Blocks.Gameplay.Core
{
    public class BloodSplatterEffect : MonoBehaviour
    {
        public static void Spawn(Vector3 position, Vector3 normal)
        {
            GameObject bloodObj = new GameObject("BloodSplatterParticle");
            bloodObj.transform.position = position;
            // Face the normal but add some spread
            if (normal != Vector3.zero)
                bloodObj.transform.rotation = Quaternion.LookRotation(normal);

            var ps = bloodObj.AddComponent<ParticleSystem>();
            // Dừng ParticleSystem trước khi cài đặt cấu hình để tránh lỗi "Setting the duration while system is still playing"
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            
            var renderer = bloodObj.GetComponent<ParticleSystemRenderer>();
            
            // Setup material (Basic red sprite)
            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = new Color(0.8f, 0f, 0f, 1f);
            renderer.material = mat;

            var main = ps.main;
            main.duration = 1f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
            main.startColor = new Color(0.7f, 0f, 0f, 1f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 1.5f;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            // Burst of blood particles
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 15, 30) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 25f;
            shape.radius = 0.1f;
            
            ps.Play();

            // Auto destroy the object after particles fade
            Destroy(bloodObj, 2f);
        }
    }
}
