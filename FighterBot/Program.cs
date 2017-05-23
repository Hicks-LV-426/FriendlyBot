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
        fleet.Plan();
        fleet.Execute();
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
      Universe Universe { get; set; }
      Dictionary<int, Ship> Ships = new Dictionary<int, Ship>();
      List<Action> DefensiveActions = new List<Action>();
      List<Action> OffensiveActions = new List<Action>();

      public void Scan(Universe universe)
      {
        Universe = universe;
        foreach (var f in universe.Factories.Where(f => f.Value.Owner == Owner.Player))
        {
          if (!Ships.ContainsKey(f.Key)) Ships.Add(f.Key, new Ship(f.Key, universe.Links));

          var ship = Ships[f.Key];
          ship.Update(f.Value, universe.Troops);
        }
      }
      public void Plan()
      {
        Defend();
        CalculateActions();
        ChooseActions();
      }

      private void Defend()
      {
        DefensiveActions.Clear();
        var needDefense = Ships.Where(s => s.Value.DefenseDeficit > 0).Select(sh => sh.Value);
        if (needDefense.Count() == 0) return;

        foreach (var ship in needDefense)
        {
          var defenders = Ships.Where(s => s.Value.ArmySize > 0).Select(s => s.Value).OrderBy(o => o.DistanceTo(ship.Id));
          foreach (var defender in defenders)
          {
            var action = defender.Defend(ship);

            if (action != null) DefensiveActions.Add(action);
            if (defender.DefenseDeficit == 0) break;
          }
        }

      }
      private void CalculateActions()
      {
        OffensiveActions.Clear();
        var army = Ships.Where(s => s.Value.ArmySize > 0).Select(v => v.Value);

        foreach (var ship in army)
        {
          if (ship.CanUpgrade) OffensiveActions.Add(new Action().Upgrade(ship));

          foreach (var d in ship.Distances)
          {
            var target = Universe.Factories[d.Key];
            if (target.Owner == Owner.Player) continue;

            var targetShip = new Ship(d.Key, Universe.Links).Update(target, Universe.Troops);
            if (target.Owner == Owner.Neutral)
              OffensiveActions.Add(new Action().Move(ship, targetShip, target.Cyborgs + 1));
          }
        }
      }
      private void ChooseActions()
      {
        throw new NotImplementedException();
      }
      private void GetTargets()
      {
        //
      }

      public void Execute()
      {
        // Any valid action, such as "WAIT" or "MOVE source destination cyborgs"
        Console.WriteLine("WAIT");
      }
      private void AddMove(int sourceId, int targetId, int size)
      {
        //
      }
    }
    class Ship
    {
      const int MAX_PRODUCTION = 3;

      public Ship(int id, List<Link> links)
      {
        Id = id;
        foreach (var link in links.Where(l => l.HasFactory(Id)))
        {
          Distances.Add(link.GetOtherId(Id), link.Distance);
        }
      }
      public int Id { get; private set; }
      public int Production { get; private set; } = 0;
      public int Population { get; private set; } = 0;
      public int DefenseDeficit { get; private set; } = 0;
      public int FrozenTurnsRemaining { get; private set; }
      public bool Frozen => FrozenTurnsRemaining > 0;
      public int ArmySize { get; private set; }
      public bool CanUpgrade => Production > MAX_PRODUCTION;

      public Dictionary<int, int> Distances = new Dictionary<int, int>();
      List<Troop> IncommingTroops = new List<Troop>();

      public Ship Update(Factory factory, List<Troop> troops)
      {
        Production = factory.Production;
        Population = factory.Cyborgs;
        FrozenTurnsRemaining = factory.TurnsFrozen;

        IncommingTroops.Clear();
        IncommingTroops.AddRange(troops.Where(t => t.TargetFactoryId == Id));

        CalculateDefense();
        return this;
      }

      private void CalculateDefense()
      {
        DefenseDeficit = 0;
        ArmySize = 0;
        var turn = 0;
        var population = Population;
        var army = 0;
        var deficit = 0;

        var incomming = IncommingTroops.OrderBy(a => a.Turns);
        foreach (var fighter in incomming)
        {
          var frozenTurnsRemaining = Frozen && turn <= FrozenTurnsRemaining ? FrozenTurnsRemaining - turn : 0;

          var turnsTillImpact = fighter.Turns - turn;
          turn += turnsTillImpact;

          if (turnsTillImpact < frozenTurnsRemaining) frozenTurnsRemaining = turnsTillImpact;

          population += Production * (turnsTillImpact - frozenTurnsRemaining);

          if (fighter.Owner == Owner.Player) population += fighter.Cyborgs;
          else population -= fighter.Cyborgs;

          if (population < 0)
          {
            var currentDeficit = Math.Abs(population) + 1;

            if (army >= currentDeficit)
            {
              army -= currentDeficit;
            }
            else
            {
              deficit += (currentDeficit - army);
              population += (currentDeficit - army);
              army = 0;
            }
          }
          if (population > 1)
          {
            army += population - 1;
          }
        }

        DefenseDeficit = deficit;
        ArmySize = army;
      }
      public int DistanceTo(int factoryId) => Distances[factoryId];
      internal Action Defend(Ship ship)
      {
        if (ship.DefenseDeficit == 0) return null;

        var cyborgs = ArmySize;
        if (ArmySize >= ship.DefenseDeficit) cyborgs = ArmySize - ship.DefenseDeficit;

        ArmySize -= cyborgs;
        ship.DefenseDeficit -= cyborgs;
        return new Action().Move(this, ship, cyborgs);
      }
    }

    struct Link
    {
      public int Factory1Id { get; set; }
      public int Factory2Id { get; set; }
      public int Distance { get; set; }
      public bool HasFactory(int factoryId) => Factory1Id == factoryId || Factory2Id == factoryId;
      public int GetDistance(int id1, int id2)
      {
        return HasFactory(id1) && HasFactory(id2) ? Distance : 0;
      }
      public int GetOtherId(int id) => Factory1Id == id ? Factory2Id : Factory1Id;
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
    class Action
    {
      public Action Move(Ship source, Ship target, int cyborgs)
      {
        Cost = GetCost(cyborgs, source.DistanceTo(target.Id), target.Population, target.Production);
        Value = $"MOVE {source.Id} {target.Id} {cyborgs}";

        return this;
      }
      public Action Upgrade(Ship source)
      {
        Cost = GetCost(10, 1, source.Population, source.Production + 1);
        Value = $"INC {source.Id}";
        return this;
      }
      public decimal Cost { get; private set; }
      public string Value { get; private set; }
      private decimal GetCost(int cyborgs, int turns, int population, int production)
      {
        return (cyborgs + turns) / ((population / turns) + production);
      }
    }
    class Target
    {
      public int SourceFactoryId { get; set; }
      public int TargetFactoryId { get; set; }
      public int Distance { get; set; }
      public int Production { get; set; }
      public int Population { get; set; }
      public Owner Owner { get; set; }
      public decimal Value { get; set; }
      public Target New(int sourceId, int targetId, int distance, int production, int population, Owner owner)
      {
        SourceFactoryId = sourceId;
        TargetFactoryId = targetId;
        Distance = distance;
        Production = production;
        Population = population;
        Owner = owner;
        //Production	Distance	Value	Population		Frozen	
        //1	1	1	10		10	0.1000
        //1	5	1	10		50	0.0200

        //Production	Distance	Value	Population		Frozen	
        //1 1 1 10    10  0.1000
        //1 5 1 10    50  0.0200

        return this;
      }
    }
    public enum Owner
    {
      Player = 1,
      Neutral = 0,
      Oponent = -1
    }
  }
}
