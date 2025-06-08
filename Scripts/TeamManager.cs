using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;

public class TeamManager : MonoBehaviour
{
    public static TeamManager Instance;

    private List<SearchAgentTeam> agents = new List<SearchAgentTeam>();

    private void Awake()
    {
        Instance = this;
    }

    public void RegisterAgent(SearchAgentTeam agent)
    {
        if (!agents.Contains(agent))
            agents.Add(agent);
    }

    public void GiveTeamReward(float reward)
    {
        foreach (var agent in agents)
        {
            agent.AddReward(reward);
        }
    }

    public void EndTeamEpisode()
    {
        foreach (var agent in agents)
        {
            agent.EndEpisode();
        }
    }

    public List<SearchAgentTeam> GetAgents()
    {
        return agents;
    }

}