﻿namespace FighterBot
{
  using System;
  using System.Linq;
  using System.IO;
  using System.Text;
  using System.Collections;
  using System.Collections.Generic;

  class Player
  {
    static void Main(string[] args)
    {
      var universe = new Universe().Initialize();
      var fleet = new Fleet();

      // game loop
      while (true)
      {
        universe.Refresh();
        fleet.Scan(universe);

        // Write an action using Console.WriteLine()
        // To debug: Console.Error.WriteLine("Debug messages...");


        // Any valid action, such as "WAIT" or "MOVE source destination cyborgs"
        Console.WriteLine("WAIT");
      }
    }

    class Universe
    {
      const string FACTORY = "FACTORY";
      const string TROOP = "TROOP";
      const string BOMB = "BOMB";

      public int FactoryCount { get; private set; }
      public List<Link> Links = new List<Link>();
      public List<Troop> Troops = new List<Troop>();
      public List<Bomb> Bombs = new List<Bomb>();
      public Dictionary<int, Factory> Factories = new Dictionary<int, Factory>();

      public Universe Initialize()
      {
        string[] inputs;
        FactoryCount = int.Parse(Console.ReadLine()); // the number of factories
        int linkCount = int.Parse(Console.ReadLine()); // the number of links between factories
        for (int i = 0; i < linkCount; i++)
        {
          inputs = Console.ReadLine().Split(' ');
          Links.Add(new Link()
          {
            Factory1Id = int.Parse(inputs[0]),
            Factory2Id = int.Parse(inputs[1]),
            Distance = int.Parse(inputs[2])
          });
        }
        return this;
      }
      public void Refresh()
      {
        Troops.Clear();
        Bombs.Clear();
        int entityCount = int.Parse(Console.ReadLine()); // the number of entities (e.g. factories and troops)

        for (int i = 0; i < entityCount; i++)
        {
          string[] inputs;
          inputs = Console.ReadLine().Split(' ');
          int entityId = int.Parse(inputs[0]);
          string entityType = inputs[1];
          switch (entityType)
          {
            case FACTORY:
              if (!Factories.ContainsKey(entityId)) { Factories.Add(entityId, new Factory()); }

              UpdateFactory(Factories[entityId], inputs);
              break;
            case TROOP:
              var troop = new Troop()
              {
                Owner = (Owner)int.Parse(inputs[2]),//arg1: player that owns the troop: 1 for you or -1 for your opponent
                SourceFactoryId = int.Parse(inputs[3]),//arg2: identifier of the factory from where the troop leaves
                TargetFactoryId = int.Parse(inputs[4]),//arg3: identifier of the factory targeted by the troop
                Cyborgs = int.Parse(inputs[5]),//arg4: number of cyborgs in the troop(positive integer)
                Turns = int.Parse(inputs[6])//arg5: remaining number of turns before the troop arrives(positive integer)
              };
              break;
            case BOMB:
              var bomb = new Bomb()
              {
                Owner = (Owner)int.Parse(inputs[2]),//arg1: player that send the bomb: 1 if it is you, -1 if it is your opponent
                SourceFactoryId = int.Parse(inputs[3]),//arg2: identifier of the factory from where the bomb is launched
                TargetFactoryId = int.Parse(inputs[4]),//arg3: identifier of the targeted factory if it's your bomb, -1 otherwise
                Turns = int.Parse(inputs[5]),//arg4: remaining number of turns before the bomb explodes(positive integer) if that's your bomb, -1 otherwise
                Arg5 = int.Parse(inputs[6])//arg5: unused
              };
              break;
            default:
              Console.Error.WriteLine($"Unknown entity type {entityType}");
              break;
          }
        }
      }
      public void UpdateFactory(Factory factory, string[] inputs)
      {
        factory.Owner = (Owner)int.Parse(inputs[2]);//arg1: player that owns the factory: 1 for you, -1 for your opponent and 0 if neutral
        factory.Cyborgs = int.Parse(inputs[3]);//arg2: number of cyborgs in the factory
        factory.Production = int.Parse(inputs[4]);//arg3: factory production(between 0 and 3)
        factory.TurnsFrozen = int.Parse(inputs[5]);//arg4: number of turns before the factory starts producing again(0 means that the factory produces normally)
        factory.Arg5 = int.Parse(inputs[6]);//arg5: unused
      }
    }
    class Fleet
    {
      Dictionary<int, Ship> Ships = new Dictionary<int, Ship>();

      public void Scan(Universe universe)
      {
        foreach (var f in universe.Factories.Where(f => f.Value.Owner == Owner.Player))
        {
          if (!Ships.ContainsKey(f.Key)) Ships.Add(f.Key, new Ship(f.Key));

          var ship = Ships[f.Key];
          ship.Update(f.Value, universe.Troops);
        }
      }
    }
    class Ship
    {
      public Ship(int id)
      {
        Id = id;
      }
      public int Id { get; private set; }
      public int Production { get; private set; } = 0;
      public int Population { get; private set; } = 0;
      public int Defense { get; private set; } = 0;
      public bool Frozen { get; private set; }
      public int Army => Population - Defense;
      List<Troop> Attackers = new List<Troop>();
      public void Update(Factory factory, List<Troop> troops)
      {
        Production = factory.Production;
        Population = factory.Cyborgs;
        Frozen = factory.TurnsFrozen > 0;

        Attackers.Clear();
        Attackers.AddRange(troops.Where(t => t.TargetFactoryId == Id));
      }
    }

    struct Link
    {
      public int Factory1Id { get; set; }
      public int Factory2Id { get; set; }
      public int Distance { get; set; }
    }
    struct Factory
    {
      public Owner Owner { get; set; }//arg1: player that owns the factory: 1 for you, -1 for your opponent and 0 if neutral
      public int Cyborgs { get; set; }//arg2: number of cyborgs in the factory
      public int Production { get; set; }//arg3: factory production(between 0 and 3)
      public int TurnsFrozen { get; set; }//arg4: number of turns before the factory starts producing again(0 means that the factory produces normally)
      public int Arg5 { get; set; }//arg5: unused
    }
    struct Troop
    {
      public Owner Owner { get; set; }//arg1: player that owns the troop: 1 for you or -1 for your opponent
      public int SourceFactoryId { get; set; }//arg2: identifier of the factory from where the troop leaves
      public int TargetFactoryId { get; set; }//arg3: identifier of the factory targeted by the troop
      public int Cyborgs { get; set; }//arg4: number of cyborgs in the troop(positive integer)
      public int Turns { get; set; }//arg5: remaining number of turns before the troop arrives(positive integer)
    }
    struct Bomb
    {
      public Owner Owner { get; set; }//arg1: player that send the bomb: 1 if it is you, -1 if it is your opponent
      public int SourceFactoryId { get; set; }//arg2: identifier of the factory from where the bomb is launched
      public int TargetFactoryId { get; set; }//arg3: identifier of the targeted factory if it's your bomb, -1 otherwise
      public int Turns { get; set; }//arg4: remaining number of turns before the bomb explodes(positive integer) if that's your bomb, -1 otherwise
      public int Arg5 { get; set; }//arg5: unused
    }
    public enum Owner
    {
      Player = 1,
      Neutral = 0,
      Oponent = -1
    }
  }
}