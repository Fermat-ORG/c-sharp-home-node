﻿using ProfileServerProtocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProfileServerSimulator
{
  /// <summary>
  /// Engine that executes the commands.
  /// </summary>
  public class CommandProcessor
  {
    private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

    /// <summary>Directory of the assembly.</summary>
    public static string BaseDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);

    /// <summary>Full path to the directory that contains files of running server instances.</summary>
    public static string InstanceDirectory = Path.Combine(BaseDirectory, "instances");

    /// <summary>Full path to the directory that contains original binaries.</summary>
    public static string BinariesDirectory = Path.Combine(BaseDirectory, "bin");

    /// <summary>Full path to the directory that contains images.</summary>
    public static string ImagesDirectory = Path.Combine(BaseDirectory, "images");

    /// <summary>Full path to the directory that contains original profile server files within binary directory.</summary>
    public static string ProfileServerBinariesDirectory = Path.Combine(BinariesDirectory, "ProfileServer");

    /// <summary>List of commands to execute.</summary>
    private List<Command> commands;

    /// <summary>List of profile server instances mapped by their name.</summary>
    private Dictionary<string, ProfileServer> profileServers = new Dictionary<string, ProfileServer>(StringComparer.Ordinal);

    /// <summary>List of identity client instances mapped by their name.</summary>
    private Dictionary<string, IdentityClient> identityClients = new Dictionary<string, IdentityClient>(StringComparer.Ordinal);

    /// <summary>
    /// Initializes the object instance.
    /// </summary>
    /// <param name="Commands">List of commands to execute.</param>
    public CommandProcessor(List<Command> Commands)
    {
      log.Trace("()");

      this.commands = Commands;

      log.Trace("(-)");
    }

    /// <summary>
    /// Frees resources used by command processor.
    /// </summary>
    public void Shutdown()
    {
      log.Trace("()");

      foreach (IdentityClient identity in identityClients.Values)
      {
        identity.Shutdown();
      }

      foreach (ProfileServer server in profileServers.Values)
      {
        server.Shutdown();
      }

      log.Trace("(-)");
    }

    /// <summary>
    /// Executes all commands.
    /// </summary>
    public void Execute()
    {
      log.Trace("()");

      ClearHistory();

      int index = 1;
      bool error = false;
      foreach (Command command in commands)
      {
        log.Info("Executing #{0:0000}@l{1}: {2}", index, command.LineNumber, command.OriginalCommand);
        index++;

        switch (command.Type)
        {
          case CommandType.ProfileServer:
            {
              CommandProfileServer cmd = (CommandProfileServer)command;
              for (int i = 1; i <= cmd.Count; i++)
              {
                string name = GetInstanceName(cmd.GroupName, i);
                GpsLocation location = Helpers.GenerateRandomGpsLocation(cmd.Latitude, cmd.Longitude, cmd.Radius);
                ProfileServer profileServer = new ProfileServer(name, location, cmd.BasePort);
                if (profileServer.Initialize())
                {
                  profileServers.Add(name, profileServer);
                }
                else
                {
                  log.Error("  * Initialization of profile server '{0}' failed.", profileServer.Name);
                  error = true;
                  break;
                }
              }

              if (!error) log.Info("  * {0} profile servers created.", cmd.Count);
              break;
            }

          case CommandType.StartServer:
            {
              CommandStartServer cmd = (CommandStartServer)command;
              for (int i = 0; i < cmd.PsCount; i++)
              {
                string name = GetInstanceName(cmd.PsGroup, cmd.PsIndex + i);
                ProfileServer profileServer;
                if (profileServers.TryGetValue(name, out profileServer))
                {
                  if (!profileServer.Start())
                  {
                    log.Error("  * Unable to start server instance '{0}'.", name);
                    error = true;
                    break;
                  }
                }
                else
                {
                  log.Error("  * Profile server instance '{0}' does not exist.", name);
                  error = true;
                  break;
                }
              }

              if (!error) log.Info("  * {0} profile servers started.", cmd.PsCount);
              break;
            }

          case CommandType.StopServer:
            {
              CommandStopServer cmd = (CommandStopServer)command;
              for (int i = 0; i < cmd.PsCount; i++)
              {
                string name = GetInstanceName(cmd.PsGroup, cmd.PsIndex + i);
                ProfileServer profileServer;
                if (profileServers.TryGetValue(name, out profileServer))
                {
                  if (!profileServer.Stop())
                  {
                    log.Error("  * Unable to stop server instance '{0}'.", name);
                    error = true;
                  }
                }
                else
                {
                  log.Error("  * Profile server instance '{0}' does not exist.", name);
                  error = true;
                  break;
                }
              }

              if (!error) log.Info("  * {0} profile servers stopped.", cmd.PsCount);
              break;
            }

          case CommandType.Identity:
            {
              CommandIdentity cmd = (CommandIdentity)command;

              List<ProfileServer> availableServers = new List<ProfileServer>();
              int availableSlots = 0;
              for (int i = 0; i < cmd.PsCount; i++)
              {
                string name = GetInstanceName(cmd.PsGroup, cmd.PsIndex + i);
                ProfileServer profileServer;
                if (profileServers.TryGetValue(name, out profileServer))
                {
                  availableServers.Add(profileServer);
                  availableSlots += profileServer.AvailableIdentitySlots;
                }
                else
                {
                  log.Error("  * Profile server instance '{0}' does not exist.", name);
                  error = true;
                  break;
                }
              }

              if (error) break;


              if (availableSlots < cmd.Count)
              {
                log.Error("  * Total number of available identity slots in selected servers is {0}, but {1} slots are required.", availableSlots, cmd.Count);
                error = true;
                break;
              }


              for (int i = 0; i < cmd.Count; i++)
              {
                string name = GetInstanceName(cmd.PsGroup, cmd.PsIndex + i);

                int serverIndex = Helpers.Rng.Next(availableServers.Count);
                ProfileServer profileServer = availableServers[serverIndex];

                GpsLocation location = Helpers.GenerateRandomGpsLocation(cmd.Latitude, cmd.Longitude, cmd.Radius);
                IdentityClient identityClient = null;
                try
                {
                  identityClient = new IdentityClient(name, cmd.IdentityType, location, cmd.ImageMask, cmd.ImageChance);
                }
                catch
                {
                  log.Error("Unable to create identity '{0}'.", name);
                  error = true;
                  break;
                }

                if (error) break;

                Task<bool> initTask = identityClient.InitializeProfileHosting(profileServer);
                if (initTask.Result) 
                {
                  profileServer.AddIdentityClient(identityClient);
                  if (profileServer.AvailableIdentitySlots == 0)
                    availableServers.RemoveAt(serverIndex);
                }
                else
                {
                  log.Error("Unable to register profile hosting and initialize profile of identity '{0}' on server '{1}'.", name, profileServer.Name);
                  error = true;
                }
              }

              if (!error) log.Info("  * {0} identities created and initialized on {1} servers.", cmd.Count, cmd.PsCount);
              break;
            }

          case CommandType.Neighborhood:
            {
              CommandNeighborhood cmd = (CommandNeighborhood)command;

              List<ProfileServer> neighborhoodList = new List<ProfileServer>();
              for (int i = 0; i < cmd.PsGroups.Count; i++)
              {
                string psGroup = cmd.PsGroups[i];
                int psCount = cmd.PsCounts[i];
                int psIndex = cmd.PsIndexes[i];
                for (int j = 0; j < psCount; j++)
                {
                  string name = GetInstanceName(psGroup, psIndex + j);

                  ProfileServer profileServer;
                  if (profileServers.TryGetValue(name, out profileServer))
                  {
                    neighborhoodList.Add(profileServer);
                  }
                  else
                  {
                    log.Error("  * Profile server instance '{0}' does not exist.", name);
                    error = true;
                    break;
                  }
                }
              }

              if (!error)
              {
                foreach (ProfileServer ps in neighborhoodList)
                {
                  if (!ps.LbnServer.AddNeighborhood(neighborhoodList))
                  {
                    log.Error("  * Unable to add neighbors to server '{0}'.", ps.Name);
                    error = true;
                    break;
                  }
                }

                if (!error)
                  log.Info("  * Neighborhood of {0} profile servers has been established.", neighborhoodList.Count);
              }
              break;
            }

          case CommandType.CancelNeighborhood:
            {
              CommandCancelNeighborhood cmd = (CommandCancelNeighborhood)command;

              List<ProfileServer> neighborhoodList = new List<ProfileServer>();
              for (int i = 0; i < cmd.PsGroups.Count; i++)
              {
                string psGroup = cmd.PsGroups[i];
                int psCount = cmd.PsCounts[i];
                int psIndex = cmd.PsIndexes[i];
                for (int j = 0; j < psCount; j++)
                {
                  string name = GetInstanceName(psGroup, psIndex + j);

                  ProfileServer profileServer;
                  if (profileServers.TryGetValue(name, out profileServer))
                  {
                    neighborhoodList.Add(profileServer);
                  }
                  else
                  {
                    log.Error("  * Profile server instance '{0}' does not exist.", name);
                    error = true;
                    break;
                  }
                }
              }

              if (!error)
              {
                foreach (ProfileServer ps in neighborhoodList)
                {
                  if (!ps.LbnServer.CancelNeighborhood(neighborhoodList))
                  {
                    log.Error("  * Unable to add neighbors to server '{0}'.", ps.Name);
                    error = true;
                    break;
                  }
                }

                if (!error)
                  log.Info("  * Neighbor relations among {0} profile servers have been cancelled.", neighborhoodList.Count);
              }
              break;
            }

          case CommandType.Neighbor:
            {
              CommandNeighbor cmd = (CommandNeighbor)command;

              ProfileServer profileServer;
              if (profileServers.TryGetValue(cmd.Source, out profileServer))
              {
                List<ProfileServer> neighborhoodList = new List<ProfileServer>();
                for (int i = 0; i < cmd.Targets.Count; i++)
                {
                  string name = cmd.Targets[i];
                  ProfileServer target;
                  if (profileServers.TryGetValue(name, out target))
                  {
                    neighborhoodList.Add(profileServer);
                  }
                  else
                  {
                    log.Error("  * Profile server instance '{0}' does not exist.", name);
                    error = true;
                    break;
                  }
                }


                if (!error)
                {
                  if (profileServer.LbnServer.AddNeighborhood(neighborhoodList))
                  {
                    log.Info("  * {0} servers have been added to the neighborhood of server '{1}'.", neighborhoodList.Count, profileServer.Name);
                  }
                  else
                  {
                    log.Error("  * Unable to add neighbors to server '{0}'.", profileServer.Name);
                    error = true;
                    break;
                  }
                }
              }
              else
              {
                log.Error("  * Profile server instance '{0}' does not exist.", cmd.Source);
                error = true;
                break;
              }

              break;
            }

          case CommandType.CancelNeighbor:
            {
              CommandCancelNeighbor cmd = (CommandCancelNeighbor)command;

              ProfileServer profileServer;
              if (profileServers.TryGetValue(cmd.Source, out profileServer))
              {
                List<ProfileServer> neighborhoodList = new List<ProfileServer>();
                for (int i = 0; i < cmd.Targets.Count; i++)
                {
                  string name = cmd.Targets[i];
                  ProfileServer target;
                  if (profileServers.TryGetValue(name, out target))
                  {
                    neighborhoodList.Add(profileServer);
                  }
                  else
                  {
                    log.Error("  * Profile server instance '{0}' does not exist.", name);
                    error = true;
                    break;
                  }
                }


                if (!error)
                {
                  if (profileServer.LbnServer.CancelNeighborhood(neighborhoodList))
                  {
                    log.Info("  * {0} servers have been removed from the neighborhood of server '{1}'.", neighborhoodList.Count, profileServer.Name);
                  }
                  else
                  {
                    log.Error("  * Unable to remove neighbors from neighborhood of server '{0}'.", profileServer.Name);
                    error = true;
                    break;
                  }
                }
              }
              else
              {
                log.Error("  * Profile server instance '{0}' does not exist.", cmd.Source);
                error = true;
                break;
              }

              break;
            }



          case CommandType.Delay:
            {
              CommandDelay cmd = (CommandDelay)command;
              log.Info("  * Waiting {0} seconds ...", cmd.Seconds);
              Thread.Sleep(TimeSpan.FromSeconds((double)cmd.Seconds));
              break;
            }

          default:
            log.Error("Invalid command type '{0}'.", command.Type);
            error = true;
            break;
        }

        if (error) break;
      }

      log.Trace("(-)");
    }

    /// <summary>
    /// Removes data from previous run.
    /// </summary>
    public void ClearHistory()
    {
      log.Trace("()");

      if (Directory.Exists(InstanceDirectory))
        Directory.Delete(InstanceDirectory, true);

      Directory.CreateDirectory(InstanceDirectory);

      log.Trace("(-)");
    }

    /// <summary>
    /// Generates instance name from a group name and an instance number.
    /// </summary>
    /// <param name="GroupName">Name of the server group.</param>
    /// <param name="InstanceNumber">Instance number.</param>
    /// <returns>Instance name.</returns>
    public string GetInstanceName(string GroupName, int InstanceNumber)
    {
      return string.Format("{0}{1:000}", GroupName, InstanceNumber);
    }


    /// <summary>
    /// Generates identity name from an identity group name and an identity number.
    /// </summary>
    /// <param name="GroupName">Name of the identity group.</param>
    /// <param name="IdentityNumber">Identity number.</param>
    /// <returns>Identity name.</returns>
    public string GetIdentityName(string GroupName, int IdentityNumber)
    {
      return string.Format("{0}{1:00000}", GroupName, IdentityNumber);
    }
  }
}
