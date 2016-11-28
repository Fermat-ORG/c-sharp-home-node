﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeNetSimulator
{
  /// <summary>
  /// All types of commands that are supported. Note that Unknown represents an invalid command.
  /// </summary>
  public enum CommandType { Unknown, ProfileServer, StartServer, StopServer, Neighborhood, Neighbor, Identity, TestQuery, Delay }

  /// <summary>
  /// Base class for all types for commands.
  /// </summary>
  public class Command
  {
    /// <summary>Type of the command.</summary>
    public CommandType Type;

    /// <summary>Line number of the command in the scenario file.</summary>
    public int LineNumber;

    /// <summary>Original scenario file line.</summary>
    public string OriginalCommand;

    /// <summary>
    /// Initializes the instance.
    /// </summary>
    /// <param name="Type">Type of the command.</param>
    /// <param name="LineNumber">Line number of the command in the scenario file.</param>
    /// <param name="OriginalCommand">Original scenario file line.</param>
    public Command(CommandType Type, int LineNumber, string OriginalCommand)
    {
      this.Type = Type;
      this.LineNumber = LineNumber;
      this.OriginalCommand = OriginalCommand;
    }
  }


  /// <summary>
  /// ProfileServer command creates one or more profile servers with associated LBN server.
  /// </summary>
  public class CommandProfileServer : Command
  {
    /// <summary>Name of the group of the servers.</summary>
    public string GroupName;

    /// <summary>Number of instances to create.</summary>
    public int Count;

    /// <summary>TCP port number from which TCP ports of each profile server and associated LBN servers are to be calculated.</summary>
    public int BasePort;

    /// <summary>GPS latitude in the decimal form of the target area centre.</summary>
    public decimal Latitude;

    /// <summary>GPS longitude in the decimal form of the target area centre.</summary>
    public decimal Longitude;

    /// <summary>Radius in metres that together with Latitude and Longitude specify the target area.</summary>
    public int Radius;

    /// <summary>
    /// Initializes the base command type.
    /// </summary>
    /// <param name="LineNumber">Line number of the command in the scenario file.</param>
    /// <param name="OriginalCommand">Original scenario file line.</param>
    public CommandProfileServer(int LineNumber, string OriginalCommand):
      base(CommandType.ProfileServer, LineNumber, OriginalCommand)
    {
    }
  }


  /// <summary>
  /// StartServer command starts one or more profile servers.
  /// </summary>
  public class CommandStartServer : Command
  {
    /// <summary>Name of the server group, which servers are going to be started.</summary>
    public string PsGroup;

    /// <summary>Index of the first server from the group.</summary>
    public int PsIndex;

    /// <summary>Number of servers to start.</summary>
    public int PsCount;

    /// <summary>
    /// Initializes the base command type.
    /// </summary>
    /// <param name="LineNumber">Line number of the command in the scenario file.</param>
    /// <param name="OriginalCommand">Original scenario file line.</param>
    public CommandStartServer(int LineNumber, string OriginalCommand) :
      base(CommandType.StartServer, LineNumber, OriginalCommand)
    {
    }
  }


  /// <summary>
  /// StopServer command stops one or more profile servers.
  /// </summary>
  public class CommandStopServer : Command
  {
    /// <summary>Name of the server group, which servers are going to be stopped.</summary>
    public string PsGroup;

    /// <summary>Index of the first server from the group.</summary>
    public int PsIndex;

    /// <summary>Number of servers to stop.</summary>
    public int PsCount;

    /// <summary>
    /// Initializes the base command type.
    /// </summary>
    /// <param name="LineNumber">Line number of the command in the scenario file.</param>
    /// <param name="OriginalCommand">Original scenario file line.</param>
    public CommandStopServer(int LineNumber, string OriginalCommand) :
      base(CommandType.StopServer, LineNumber, OriginalCommand)
    {
    }
  }


  /// <summary>
  /// Neighborhood command forms a bidirectional neighborhood relationship between all servers selected by the command. 
  /// </summary>
  public class CommandNeighborhood : Command
  {
    /// <summary>Names of the groups of servers.</summary>
    public List<string> PsGroups;

    /// <summary>Instance numbers of the first servers from the groups.</summary>
    public List<int> PsIndexes;

    /// <summary>Number of servers to take from the groups.</summary>
    public List<int> PsCounts;

    /// <summary>
    /// Initializes the base command type.
    /// </summary>
    /// <param name="LineNumber">Line number of the command in the scenario file.</param>
    /// <param name="OriginalCommand">Original scenario file line.</param>
    public CommandNeighborhood(int LineNumber, string OriginalCommand):
      base(CommandType.Neighborhood, LineNumber, OriginalCommand)
    {
    }
  }


  /// <summary>
  /// Neighbor command forms an unidirectional neighborhood relationship between a source server and one or more target servers.
  /// </summary>
  public class CommandNeighbor : Command
  {
    /// <summary>Name of the source server instance.</summary>
    public string Source;

    /// <summary>Names of target servers instances.</summary>
    public List<string> Targets;

    /// <summary>
    /// Initializes the base command type.
    /// </summary>
    /// <param name="LineNumber">Line number of the command in the scenario file.</param>
    /// <param name="OriginalCommand">Original scenario file line.</param>
    public CommandNeighbor(int LineNumber, string OriginalCommand) :
      base(CommandType.Neighbor, LineNumber, OriginalCommand)
    {
    }
  }


  /// <summary>
  /// Identity command spawns one or more identities.
  /// </summary>
  public class CommandIdentity : Command
  {
    /// <summary>Name of the identity group.</summary>
    public string Name;

    /// <summary>Number of instances to create.</summary>
    public int Count;

    /// <summary>Identity type.</summary>
    public string IdentityType;

    /// <summary>GPS latitude in the decimal form of the target area centre.</summary>
    public decimal Latitude;

    /// <summary>GPS longitude in the decimal form of the target area centre.</summary>
    public decimal Longitude;

    /// <summary>Radius in metres that together with Latitude and Longitude specify the target area.</summary>
    public int Radius;

    /// <summary>File name mask in the image folder that define which images can be randomly selected for identity profiles.</summary>
    public string ImageMask;

    /// <summary>An integer between 0 and 100 that specifies the chance of each instance to have a profile image set.</summary>
    public int ImageChance;

    /// <summary>Name of the server group, which servers are going to host the newly created identities.</summary>
    public string PsGroup;

    /// <summary>Index of the first server from the group.</summary>
    public int PsIndex;

    /// <summary>Number of servers to take from the group.</summary>
    public int PsCount;


    /// <summary>
    /// Initializes the base command type.
    /// </summary>
    /// <param name="LineNumber">Line number of the command in the scenario file.</param>
    /// <param name="OriginalCommand">Original scenario file line.</param>
    public CommandIdentity(int LineNumber, string OriginalCommand) :
      base(CommandType.Identity, LineNumber, OriginalCommand)
    {
    }
  }

  /// <summary>
  /// TestQuery command performs one or more search queries against specific server.
  /// </summary>
  public class CommandTestQuery: Command
  {
    /// <summary>Name of the server group, which servers are going to be queried.</summary>
    public string PsGroup;

    /// <summary>Index of the first server from the group.</summary>
    public int PsIndex;

    /// <summary>Number of servers to take from the group.</summary>
    public int PsCount;

    /// <summary>Wildcard profile name filter for the search query.</summary>
    public string NameFilter;

    /// <summary>Wildcard profile type filter for the search query.</summary>
    public string TypeFilter;

    /// <summary>True if the query should request profile images, false otherwise.</summary>
    public bool IncludeImages;

    /// <summary>GPS latitude in the decimal form of the target area centre.</summary>
    public decimal Latitude;

    /// <summary>GPS longitude in the decimal form of the target area centre.</summary>
    public decimal Longitude;

    /// <summary>If Latitude is not "NO_LOCATION", this is radius in metres that together with Latitude and Longitude specify the target area</summary>
    public int Radius;


    /// <summary>
    /// Initializes the base command type.
    /// </summary>
    /// <param name="LineNumber">Line number of the command in the scenario file.</param>
    /// <param name="OriginalCommand">Original scenario file line.</param>
    public CommandTestQuery(int LineNumber, string OriginalCommand) :
      base(CommandType.TestQuery, LineNumber, OriginalCommand)
    {
    }
  }

  /// <summary>
  /// Delay command waits specified amount of time before executing next command.
  /// </summary>
  public class CommandDelay : Command
  {
    /// <summary>Number of seconds to wait as a positive decimal number.</summary>
    public decimal Seconds;

    /// <summary>
    /// Initializes the base command type.
    /// </summary>
    /// <param name="LineNumber">Line number of the command in the scenario file.</param>
    /// <param name="OriginalCommand">Original scenario file line.</param>
    public CommandDelay(int LineNumber, string OriginalCommand) :
      base(CommandType.Delay, LineNumber, OriginalCommand)
    {
    }
  }
}