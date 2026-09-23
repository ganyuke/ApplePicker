using UnityEngine;

public static class AppleTypeParticles
{
    private static Material particleMaterial;

    public static ParticleSystem AttachGolden(Transform parent)
    {
        ParticleSystem ps = CreateChildSystem(parent, "GoldenSparkle");
        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.startLifetime = 0.35f;
        main.startSpeed = 0.12f;
        main.startSize = 0.07f;
        main.maxParticles = 6;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startColor = new Color(1f, 0.88f, 0.25f, 0.85f);

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 2f;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.3f;

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

        return ps;
    }

    public static ParticleSystem AttachPoison(Transform parent)
    {
        ParticleSystem ps = CreateChildSystem(parent, "PoisonCloud");
        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.startLifetime = 1.1f;
        main.startSpeed = 0.05f;
        main.startSize = 0.2f;
        main.maxParticles = 10;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startColor = new Color(0.55f, 0.15f, 0.75f, 0.28f);

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 3f;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.35f;

        ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.y = 0.08f;

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.6f, 1f, 1.1f));

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.55f, 0.15f, 0.75f), 0f),
                new GradientColorKey(new Color(0.45f, 0.1f, 0.65f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.3f, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradient;

        return ps;
    }

    private static ParticleSystem CreateChildSystem(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.Play();
        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        if (particleMaterial == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader != null) particleMaterial = new Material(shader);
        }
        if (particleMaterial != null) renderer.sharedMaterial = particleMaterial;
        return ps;
    }
}
