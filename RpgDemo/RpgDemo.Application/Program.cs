using System;
using System.Reflection;

namespace RpgDemo.Application;

class Weapon
{
    public Weapon(string name, int damage)
    {
        Name = name;
        Damage = damage > 0 ? damage : throw new ArgumentException("Ungültiger Schaden");
    }

    public string Name { get; }
    public int Damage { get; }
    public string DisplayName => $"{Name} (+{Damage})";
}

class Character
{
    private int health;

    public Character(string name, int maxHealth, int strength)
    {
        Name = name;
        MaxHealth = maxHealth > 0 ? maxHealth : throw new ArgumentException("Ungültige MaxHealth");
        Strength = strength;
        Health = MaxHealth;
    }

    public string Name { get; }
    public int MaxHealth { get; }
    public int Strength { get; }

    public int Health
    {
        get => health;
        private set => health = Math.Clamp(value, 0, MaxHealth);
    }

    public bool IsAlive => Health > 0;
    public Weapon? Weapon { get; set; }
    public int AttackPower => Strength + (Weapon?.Damage ?? 0);
    public int Experience { get; private set; } = 0;
    public int Level => 1 + Experience / 100;

    public void TakeDamage(int damage)
    {
        if (damage < 0) throw new ArgumentException("Ungültiger Schaden");
        Health -= damage;
    }

    public void Heal(int amount)
    {
        if (!IsAlive) return;
        Health += amount;
    }

    public void GainExperience(int points)
    {
        if (points < 0) throw new ArgumentException("Ungültige Erfahrung");
        Experience += points;
    }

    public void Attack(Character target)
    {
        if (!IsAlive || !target.IsAlive) return;
        target.TakeDamage(AttackPower);
        if (!target.IsAlive) Experience += 50;
    }
}

class Program
{
    // DON'T TOUCH!
    private static void Main(string[] args)
    {
        Console.WriteLine("********************************************************************************");
        Console.WriteLine("TESTS FÜR WEAPON");
        Console.WriteLine("********************************************************************************");
        if (typeof(Weapon).GetConstructor(Type.EmptyTypes) is null) { Console.WriteLine("1 Kein default Konstruktor OK"); }
        if (IsReadOnly(typeof(Weapon), nameof(Weapon.Name)) && IsReadOnly(typeof(Weapon), nameof(Weapon.Damage)))
        {
            Console.WriteLine("2 Name und Damage sind immutable OK");
        }
        Weapon sword = new Weapon(name: "Schwert", damage: 15);
        if (IsReadOnly(typeof(Weapon), nameof(Weapon.DisplayName)) && sword.DisplayName == "Schwert (+15)")
        {
            Console.WriteLine("3 DisplayName OK");
        }
        try
        {
            Weapon stick = new Weapon(name: "Stock", damage: 0);
        }
        catch (ArgumentException)
        {
            Console.WriteLine("4 Exception bei ungültigem Schaden OK");
        }

        Console.WriteLine("********************************************************************************");
        Console.WriteLine("TESTS FÜR CHARACTER");
        Console.WriteLine("********************************************************************************");
        if (typeof(Character).GetConstructor(Type.EmptyTypes) is null) { Console.WriteLine("1 Kein default Konstruktor OK"); }
        if (IsReadOnly(typeof(Character), nameof(Character.Name))
            && IsReadOnly(typeof(Character), nameof(Character.MaxHealth))
            && IsReadOnly(typeof(Character), nameof(Character.Strength)))
        {
            Console.WriteLine("2 Name, MaxHealth und Strength sind immutable OK");
        }
        Character hero = new Character(name: "Link", maxHealth: 100, strength: 10);
        if (hero.Name == "Link" && hero.MaxHealth == 100 && hero.Strength == 10 && hero.Health == 100 && hero.IsAlive)
        {
            Console.WriteLine("3 Werte aus dem Konstruktor, Health startet mit MaxHealth OK");
        }
        if (HasPrivateSetter(typeof(Character), nameof(Character.Health))
            && HasPrivateSetter(typeof(Character), nameof(Character.Experience)))
        {
            Console.WriteLine("4 Health und Experience sind von außen nicht setzbar OK");
        }
        try
        {
            Character ghost = new Character(name: "Ghost", maxHealth: 0, strength: 10);
        }
        catch (ArgumentException)
        {
            Console.WriteLine("5 Exception bei ungültiger MaxHealth OK");
        }

        hero.TakeDamage(30);
        if (hero.Health == 70) { Console.WriteLine("6 TakeDamage OK"); }
        try
        {
            hero.TakeDamage(-10);
        }
        catch (ArgumentException)
        {
            if (hero.Health == 70) { Console.WriteLine("7 Exception bei negativem Schaden OK"); }
        }
        hero.Heal(20);
        int healthAfterSmallHeal = hero.Health;
        hero.Heal(500);
        if (healthAfterSmallHeal == 90 && hero.Health == 100) { Console.WriteLine("8 Heal OK"); }

        Character dummy = new Character(name: "Dummy", maxHealth: 50, strength: 10);
        dummy.TakeDamage(80);
        dummy.Heal(10);
        if (dummy.Health == 0 && !dummy.IsAlive && IsReadOnly(typeof(Character), nameof(Character.IsAlive)))
        {
            Console.WriteLine("9 Health wird nicht negativ, Tote werden nicht geheilt OK");
        }

        int attackPowerWithoutWeapon = hero.AttackPower;
        hero.Weapon = sword;
        if (IsReadOnly(typeof(Character), nameof(Character.AttackPower))
            && attackPowerWithoutWeapon == 10 && hero.AttackPower == 25)
        {
            Console.WriteLine("10 AttackPower OK");
        }

        hero.GainExperience(250);
        if (IsReadOnly(typeof(Character), nameof(Character.Level)) && hero.Experience == 250 && hero.Level == 3)
        {
            Console.WriteLine("11 Experience und Level OK");
        }
        try
        {
            hero.GainExperience(-1);
        }
        catch (ArgumentException)
        {
            Console.WriteLine("12 Exception bei negativer Erfahrung OK");
        }

        // The hero hits the orc 3 times with an attack power of 25 (60 -> 35 -> 10 -> 0).
        Character orc = new Character(name: "Ork", maxHealth: 60, strength: 25);
        hero.Attack(orc);
        int orcHealthAfterFirstAttack = orc.Health;
        hero.Attack(orc);
        hero.Attack(orc);
        // A dead orc cannot be attacked, so the hero gets the 50 points only once.
        hero.Attack(orc);
        if (orcHealthAfterFirstAttack == 35 && !orc.IsAlive && hero.Experience == 300)
        {
            Console.WriteLine("13 Attack OK");
        }
    }

    private static bool IsReadOnly(Type type, string propertyName)
    {
        PropertyInfo? property = type.GetProperty(propertyName);
        return property is not null && !property.CanWrite;
    }

    private static bool HasPrivateSetter(Type type, string propertyName)
    {
        PropertyInfo? property = type.GetProperty(propertyName);
        return property is not null && property.SetMethod is not null && !property.SetMethod.IsPublic;
    }
}
