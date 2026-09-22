using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Run in the EditMode tab. These tests enter Play Mode to exercise the actual scene and PhysX.
public class AppleGameplayTests
{
    private ApplePicker picker;
    private AppleTree tree;
    private ScoreCounter score;
    private DayNightCycle cycle;
    private bool hadHighScore;
    private int savedHighScore;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        yield return new EnterPlayMode();
        hadHighScore = PlayerPrefs.HasKey("HighScore");
        savedHighScore = PlayerPrefs.GetInt("HighScore");
        Time.timeScale = 1f;
        SceneManager.LoadScene("_Scene_0");
        yield return null;
        yield return null;
        picker = Object.FindAnyObjectByType<ApplePicker>();
        tree = Object.FindAnyObjectByType<AppleTree>();
        score = Object.FindAnyObjectByType<ScoreCounter>();
        cycle = Object.FindAnyObjectByType<DayNightCycle>();
        Assert.That(picker, Is.Not.Null);
        Assert.That(tree, Is.Not.Null);
        Assert.That(cycle, Is.Not.Null);
        tree.CancelInvoke();
        tree.speed = 0f;
        tree.changeDirChance = 0f;
        cycle.enabled = false;
        picker.enabled = false;
        foreach (GameObject basket in picker.basketList) basket.GetComponent<Basket>().enabled = false;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Time.timeScale = 1f;
        if (hadHighScore) PlayerPrefs.SetInt("HighScore", savedHighScore);
        else PlayerPrefs.DeleteKey("HighScore");
        yield return new ExitPlayMode();
    }

    private Apple Spawn(AppleType type, Vector3 position)
    {
        GameObject instance = Object.Instantiate(tree.applePrefab, position, Quaternion.identity);
        Apple apple = instance.GetComponent<Apple>();
        apple.Initialize(type);
        return apple;
    }

    private Apple HitTree()
    {
        Apple apple = Spawn(AppleType.Normal, new Vector3(0f, 30f, 0f));
        apple.MarkReturned();
        Assert.That(tree.TryHit(apple), Is.True);
        return apple;
    }

    private void Unlock()
    {
        score.AddPoints(5000 - score.score);
        Object.FindAnyObjectByType<Button>().onClick.Invoke();
        Assert.That(picker.ShieldingUnlocked, Is.True);
    }

    [UnityTest]
    public IEnumerator FreshApplesSpawnInsideTreeAndOnlyReturnedApplesHit()
    {
        int normalLayer = LayerMask.NameToLayer("Apple");
        int returnedLayer = LayerMask.NameToLayer("ReturnedApple");
        int treeLayer = LayerMask.NameToLayer("AppleTree");
        Assert.That(Physics.GetIgnoreLayerCollision(normalLayer, treeLayer), Is.True);
        Assert.That(Physics.GetIgnoreLayerCollision(returnedLayer, treeLayer), Is.False);
        Assert.That(Physics.GetIgnoreLayerCollision(normalLayer, returnedLayer), Is.True);
        Assert.That(Physics.GetIgnoreLayerCollision(returnedLayer, returnedLayer), Is.True);
        tree.SendMessage("DropApple");
        tree.CancelInvoke();
        Apple fresh = Object.FindAnyObjectByType<Apple>();
        Assert.That(fresh.transform.position, Is.EqualTo(tree.transform.position));
        Assert.That(fresh.gameObject.layer, Is.EqualTo(normalLayer));
        Assert.That(tree.TryHit(fresh), Is.False);
        yield return new WaitForSeconds(0.35f);
        Assert.That(tree.Health, Is.EqualTo(100f));
        Assert.That(fresh.IsResolved, Is.False);
        fresh.TryConsume();

        // Hit the existing compound tree collider with a fast upward projectile.
        Apple returned = Spawn(AppleType.Golden, tree.transform.position + Vector3.down * 5f);
        returned.MarkReturned();
        Assert.That(returned.CompareTag("Apple"), Is.True);
        Assert.That(returned.type, Is.EqualTo(AppleType.Golden));
        returned.GetComponent<Rigidbody>().linearVelocity = Vector3.up * 50f;
        yield return new WaitForSeconds(0.15f);
        Assert.That(tree.Health, Is.EqualTo(75f));
        Assert.That(tree.IsStunned, Is.True);
        Assert.That(returned == null || returned.IsResolved, Is.True);
        Assert.That(score.score, Is.Zero);
    }

    [UnityTest]
    public IEnumerator CycleControlsSpeedAndSpawnDistributionWithoutCompounding()
    {
        Assert.That(cycle.cycleDurationInSeconds, Is.EqualTo(120f));
        tree.speed = -10f;
        for (int phase = 0; phase < 4; phase++)
        {
            bool night = phase % 2 == 1;
            cycle.transform.rotation = Quaternion.Euler(night ? 270f : 90f, 0f, 0f);
            Assert.That(cycle.IsNight, Is.EqualTo(night));
            Assert.That(tree.CurrentSpeed, Is.EqualTo(night ? -5f : -10f));
            int poison = 0, gold = 0, normal = 0;
            for (int i = 0; i < 1000; i++)
            {
                AppleType type = tree.ChooseAppleType((i + 0.5f) / 1000f);
                if (type == AppleType.Poison) poison++;
                else if (type == AppleType.Golden) gold++;
                else normal++;
            }
            Assert.That(poison, Is.EqualTo(night ? 250 : 50));
            Assert.That(gold, Is.EqualTo(night ? 50 : 150));
            Assert.That(normal, Is.EqualTo(night ? 700 : 800));
        }
        HitTree();
        Assert.That(tree.CurrentSpeed, Is.Zero);
        for (int i = 0; i < 100; i++) Assert.That(tree.ChooseAppleType(i / 100f), Is.EqualTo(AppleType.Normal));
        yield return null;
    }

    [UnityTest]
    public IEnumerator CatchesAwardPointsAndPoisonRemovesOnlyOneBasket()
    {
        Vector3 catchPosition = picker.basketList[2].transform.position + Vector3.up * 1.2f;
        Spawn(AppleType.Normal, catchPosition);
        yield return new WaitForSeconds(0.4f);
        Assert.That(score.score, Is.EqualTo(100));
        Spawn(AppleType.Golden, catchPosition);
        yield return new WaitForSeconds(0.4f);
        Assert.That(score.score, Is.EqualTo(600));
        Spawn(AppleType.Poison, catchPosition);
        Spawn(AppleType.Poison, catchPosition + Vector3.right);
        yield return new WaitForSeconds(0.4f);
        Assert.That(picker.basketList.Count, Is.EqualTo(2));
        Assert.That(score.score, Is.EqualTo(600));
        Assert.That(Object.FindObjectsByType<Apple>(FindObjectsSortMode.None), Is.Empty);
    }

    [UnityTest]
    public IEnumerator SpecialMissesAreHarmlessAndSimultaneousNormalMissesLoseOneBasket()
    {
        Spawn(AppleType.Poison, Vector3.down * 25f);
        Spawn(AppleType.Golden, Vector3.down * 25f);
        yield return null;
        yield return null;
        Assert.That(picker.basketList.Count, Is.EqualTo(3));
        Spawn(AppleType.Normal, Vector3.down * 25f).MarkReturned();
        Spawn(AppleType.Normal, Vector3.down * 25f);
        yield return null;
        yield return null;
        Assert.That(picker.basketList.Count, Is.EqualTo(2));
    }

    [UnityTest]
    public IEnumerator RewardCrossingPausesUntilButtonAndShieldsResizeWithTheStack()
    {
        picker.AppleMissed();
        Assert.That(picker.basketList.Count, Is.EqualTo(2));
        score.AddPoints(4900);
        score.AddPoints(500);
        Assert.That(picker.basketList.Count, Is.EqualTo(3));
        Assert.That(picker.NextBasketRewardScore, Is.EqualTo(10000));
        Assert.That(picker.IsShieldPromptOpen, Is.True);
        Assert.That(Time.timeScale, Is.Zero);
        float clock = Time.time;
        Vector3 treePosition = tree.transform.position;
        Quaternion lightRotation = cycle.transform.rotation;
        cycle.enabled = true;
        yield return new WaitForSecondsRealtime(0.1f);
        Assert.That(Time.time, Is.EqualTo(clock));
        Assert.That(tree.transform.position, Is.EqualTo(treePosition));
        Assert.That(cycle.transform.rotation, Is.EqualTo(lightRotation));
        Button button = Object.FindAnyObjectByType<Button>();
        Assert.That(button.GetComponentInChildren<TMPro.TextMeshProUGUI>().text, Is.EqualTo("You obtained SHIELDING"));
        button.onClick.Invoke();
        Assert.That(Time.timeScale, Is.EqualTo(1f));
        Assert.That(picker.ShieldingUnlocked, Is.True);
        Assert.That(Object.FindObjectsByType<AppleBounceSurface>(FindObjectsSortMode.None).Length, Is.EqualTo(4));
        Transform shield = GameObject.Find("Left Shield").transform;
        float fullHeight = shield.localScale.y;
        yield return null;
        picker.AppleMissed();
        Assert.That(shield.localScale.y, Is.EqualTo(fullHeight - picker.basketSpacingY).Within(0.001f));
        score.AddPoints(4700); // Cross 10,000 without landing on it exactly.
        Assert.That(picker.basketList.Count, Is.EqualTo(3));
        Assert.That(shield.localScale.y, Is.EqualTo(fullHeight).Within(0.001f));
        score.AddPoints(10000); // Consume the 20,000 reward at capacity.
        Assert.That(picker.NextBasketRewardScore, Is.EqualTo(40000));
        Assert.That(picker.IsShieldPromptOpen, Is.False);
        Assert.That(picker.basketList.Count, Is.EqualTo(3));
    }

    [UnityTest]
    public IEnumerator PadAndMovingShieldReflectWithoutCollectingTheApple()
    {
        Unlock();
        Transform pad = GameObject.Find("Left Bounce Pad").transform;
        Apple apple = Spawn(AppleType.Golden, pad.position + pad.up * 2f);
        apple.GetComponent<Rigidbody>().linearVelocity = -pad.up * 15f;
        yield return new WaitForSeconds(0.15f);
        Assert.That(apple != null && apple.IsReturned, Is.True);
        Assert.That(Vector3.Dot(apple.GetComponent<Rigidbody>().linearVelocity, pad.up), Is.GreaterThan(0f));
        Assert.That(score.score, Is.EqualTo(5000));
        apple.TryConsume();

        Rigidbody shield = GameObject.Find("Left Shield").GetComponent<Rigidbody>();
        Apple hitByShield = Spawn(AppleType.Normal, shield.position + Vector3.left * 1.5f);
        hitByShield.GetComponent<Rigidbody>().useGravity = false;
        for (int i = 0; i < 5; i++)
        {
            shield.MovePosition(shield.position + Vector3.left * 0.3f);
            yield return new WaitForFixedUpdate();
        }
        Assert.That(hitByShield != null && hitByShield.IsReturned, Is.True);
        Assert.That(hitByShield.GetComponent<Rigidbody>().linearVelocity.x, Is.LessThan(-1f));
        Assert.That(score.score, Is.EqualTo(5000));
    }

    [UnityTest]
    public IEnumerator StunLimitDoesNotPreventDamageAndExpiresAfterTheWindow()
    {
        tree.hitDamage = 1f;
        tree.recoveryPerSecond = 0f;
        tree.stunDuration = 0.06f;
        tree.stunWindow = 0.5f;
        HitTree(); HitTree(); HitTree();
        yield return new WaitForSeconds(0.1f);
        Assert.That(tree.IsStunned, Is.False);
        HitTree();
        Assert.That(tree.Health, Is.EqualTo(96f));
        Assert.That(tree.IsStunned, Is.False);
        yield return new WaitForSeconds(0.5f);
        HitTree();
        Assert.That(tree.IsStunned, Is.True);
        Assert.That(tree.Health, Is.EqualTo(95f));
    }

    [UnityTest]
    public IEnumerator DamageDarkensTreeAndRecoveryWaitsThenRestoresHealth()
    {
        tree.recoveryDelay = 0.15f;
        tree.recoveryPerSecond = 100f;
        Renderer renderer = tree.GetComponentInChildren<Renderer>();
        Color original = renderer.sharedMaterial.color;
        Apple apple = HitTree();
        Assert.That(tree.TryHit(apple), Is.False);
        Assert.That(tree.Health, Is.EqualTo(75f));
        var properties = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(properties);
        Color damaged = properties.GetColor("_BaseColor");
        Assert.That(damaged.maxColorComponent, Is.LessThan(original.maxColorComponent));
        yield return new WaitForSeconds(0.05f);
        Assert.That(tree.Health, Is.EqualTo(75f));
        yield return new WaitForSeconds(0.5f);
        Assert.That(tree.Health, Is.EqualTo(100f));
        renderer.GetPropertyBlock(properties);
        Assert.That(properties.GetColor("_BaseColor"), Is.EqualTo(original));
    }

    [UnityTest]
    public IEnumerator DeathStopsGameplayAndRestartResetsTheRun()
    {
        Unlock();
        HitTree(); HitTree(); HitTree(); HitTree();
        Assert.That(tree.IsDead, Is.True);
        Assert.That(picker.IsGameOver, Is.True);
        Assert.That(Time.timeScale, Is.Zero);
        yield return null; // Allow the previous unlock modal's deferred destruction.
        Button restart = Object.FindAnyObjectByType<Button>();
        Assert.That(restart.GetComponentInChildren<TMPro.TextMeshProUGUI>().text, Is.EqualTo("Restart"));
        restart.onClick.Invoke();
        yield return null;
        yield return null;
        picker = Object.FindAnyObjectByType<ApplePicker>();
        tree = Object.FindAnyObjectByType<AppleTree>();
        Assert.That(Time.timeScale, Is.EqualTo(1f));
        Assert.That(tree.Health, Is.EqualTo(100f));
        Assert.That(picker.ShieldingUnlocked, Is.False);
        Assert.That(picker.basketList.Count, Is.EqualTo(3));
        Assert.That(Object.FindAnyObjectByType<ScoreCounter>().score, Is.Zero);
        Assert.That(PlayerPrefs.GetInt("HighScore"), Is.GreaterThanOrEqualTo(5000));
    }

    [UnityTest]
    public IEnumerator LastBasketLossUsesTheExistingSceneRestart()
    {
        picker.AppleMissed();
        yield return null;
        picker.AppleMissed();
        yield return null;
        picker.AppleMissed();
        yield return null;
        yield return null;
        picker = Object.FindAnyObjectByType<ApplePicker>();
        Assert.That(picker.basketList.Count, Is.EqualTo(3));
        Assert.That(Time.timeScale, Is.EqualTo(1f));
        Assert.That(picker.ShieldingUnlocked, Is.False);
    }
}
