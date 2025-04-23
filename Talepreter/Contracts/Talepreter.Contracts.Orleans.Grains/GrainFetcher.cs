using Talepreter.Exceptions;

namespace Talepreter.Contracts.Orleans.Grains;

public static class GrainFetcher
{
    public static ITaleGrain FetchTale(this IGrainFactory factory, Guid taleId) => factory.GetGrain<ITaleGrain>(FetchTale(taleId));
    public static IPublishGrain FetchPublish(this IGrainFactory factory, Guid taleId, Guid taleVersionId) => factory.GetGrain<IPublishGrain>(FetchPublish(taleId, taleVersionId));
    public static IChapterGrain FetchChapter(this IGrainFactory factory, Guid taleId, Guid taleVersionId, int chapterId) => factory.GetGrain<IChapterGrain>(FetchChapter(taleId, taleVersionId, chapterId));
    public static IPageGrain FetchPage(this IGrainFactory factory, Guid taleId, Guid taleVersionId, int chapterId, int pageId) => factory.GetGrain<IPageGrain>(FetchPage(taleId, taleVersionId, chapterId, pageId));

    // --

    public static IActorContainerGrain FetchActorContainer(this IGrainFactory factory, Guid taleId, Guid taleVersionId) => factory.GetGrain<IActorContainerGrain>(FetchActorContainer(taleId, taleVersionId));
    public static IAnecdoteContainerGrain FetchAnecdoteContainer(this IGrainFactory factory, Guid taleId, Guid taleVersionId) => factory.GetGrain<IAnecdoteContainerGrain>(FetchAnecdoteContainer(taleId, taleVersionId));
    public static IPersonContainerGrain FetchPersonContainer(this IGrainFactory factory, Guid taleId, Guid taleVersionId) => factory.GetGrain<IPersonContainerGrain>(FetchPersonContainer(taleId, taleVersionId));
    public static IWorldContainerGrain FetchWorldContainer(this IGrainFactory factory, Guid taleId, Guid taleVersionId) => factory.GetGrain<IWorldContainerGrain>(FetchWorldContainer(taleId, taleVersionId));

    // --

    public static IActorExtensionGrain FetchActorExtension(this IGrainFactory factory, Guid taleId, Guid taleVersionId, string type, string extensionId) => factory.GetGrain<IActorExtensionGrain>(FetchActorExtension(taleId, taleVersionId, type, extensionId));
    public static IPersonExtensionGrain FetchPersonExtension(this IGrainFactory factory, Guid taleId, Guid taleVersionId, string type, string extensionId) => factory.GetGrain<IPersonExtensionGrain>(FetchPersonExtension(taleId, taleVersionId, type, extensionId));
    public static IAnecdoteExtensionGrain FetchAnecdoteExtension(this IGrainFactory factory, Guid taleId, Guid taleVersionId, string type, string extensionId) => factory.GetGrain<IAnecdoteExtensionGrain>(FetchAnecdoteExtension(taleId, taleVersionId, type, extensionId));

    // --

    public static IActorGrain FetchActor(this IGrainFactory factory, Guid taleId, Guid taleVersionId, string target) => factory.GetGrain<IActorGrain>(FetchActor(taleId, taleVersionId, target));
    public static IEquipmentGrain FetchEquipment(this IGrainFactory factory, Guid taleId, Guid taleVersionId, string target) => factory.GetGrain<IEquipmentGrain>(FetchEquipment(taleId, taleVersionId, target));
    public static ICohortGrain FetchCohort(this IGrainFactory factory, Guid taleId, Guid taleVersionId, string target) => factory.GetGrain<ICohortGrain>(FetchCohort(taleId, taleVersionId, target));
    public static IAnecdoteGrain FetchAnecdote(this IGrainFactory factory, Guid taleId, Guid taleVersionId, string target) => factory.GetGrain<IAnecdoteGrain>(FetchAnecdote(taleId, taleVersionId, target));
    public static IPersonGrain FetchPerson(this IGrainFactory factory, Guid taleId, Guid taleVersionId, string target) => factory.GetGrain<IPersonGrain>(FetchPerson(taleId, taleVersionId, target));
    public static ISettlementGrain FetchSettlement(this IGrainFactory factory, Guid taleId, Guid taleVersionId, string target) => factory.GetGrain<ISettlementGrain>(FetchSettlement(taleId, taleVersionId, target));
    public static IWorldGrain FetchWorld(this IGrainFactory factory, Guid taleId, Guid taleVersionId) => factory.GetGrain<IWorldGrain>(FetchWorld(taleId, taleVersionId));
    public static IRaceGrain FetchRace(this IGrainFactory factory, Guid taleId, Guid taleVersionId, string target) => factory.GetGrain<IRaceGrain>(FetchRace(taleId, taleVersionId, target));
    public static IGroupGrain FetchGroup(this IGrainFactory factory, Guid taleId, Guid taleVersionId, string target) => factory.GetGrain<IGroupGrain>(FetchGroup(taleId, taleVersionId, target));
    public static IFactionGrain FetchFaction(this IGrainFactory factory, Guid taleId, Guid taleVersionId, string target) => factory.GetGrain<IFactionGrain>(FetchFaction(taleId, taleVersionId, target));
    public static ICacheGrain FetchCache(this IGrainFactory factory, Guid taleId, Guid taleVersionId, string target) => factory.GetGrain<ICacheGrain>(FetchCache(taleId, taleVersionId, target));

    // -- ID preparers

    public static string FetchTale(Guid taleId)
    {
        if (Guid.Empty == taleId) throw new GrainIdException("<ITaleGrain> Tale id is empty guid");
        return $"TALE:{taleId:N}";
    }

    public static string FetchPublish(Guid taleId, Guid taleVersionId)
    {
        if (Guid.Empty == taleId) throw new GrainIdException("<IPublishGrain> Tale id is empty guid");
        if (Guid.Empty == taleVersionId) throw new GrainIdException("<IPublishGrain> Tale version id is empty guid");
        return $"PUBLISH:{taleVersionId:N}";
    }

    public static string FetchChapter(Guid taleId, Guid taleVersionId, int chapterId)
    {
        if (Guid.Empty == taleId) throw new GrainIdException("<IChapterGrain> Tale id is empty guid");
        if (Guid.Empty == taleVersionId) throw new GrainIdException("<IChapterGrain> Tale version id is empty guid");
        if (chapterId < 0) throw new GrainIdException("<IChapterGrain> Chapter id is negative number");
        return $"CHAPTER:{taleVersionId}\\#{chapterId}";
    }

    public static string FetchPage(Guid taleId, Guid taleVersionId, int chapterId, int pageId)
    {
        if (Guid.Empty == taleId) throw new GrainIdException("<IPageGrain> Tale id is empty guid");
        if (Guid.Empty == taleVersionId) throw new GrainIdException("<IPageGrain> Tale version id is empty guid");
        if (chapterId < 0) throw new GrainIdException("<IPageGrain> Chapter id is negative number");
        if (pageId < 0) throw new GrainIdException("<IPageGrain> Page id is negative number");
        return $"PAGE:{taleVersionId}\\#{chapterId}.#{pageId}";
    }

    // --

    public static string FetchActorContainer(Guid taleId, Guid taleVersionId)
    {
        if (Guid.Empty == taleId) throw new GrainIdException("<IActorContainerGrain> Tale id is empty guid");
        if (Guid.Empty == taleVersionId) throw new GrainIdException("<IActorContainerGrain> Tale version id is empty guid");
        return $"{taleVersionId}\\ActorContainer";
    }

    public static string FetchAnecdoteContainer(Guid taleId, Guid taleVersionId)
    {
        if (Guid.Empty == taleId) throw new GrainIdException("<IAnecdoteContainerGrain> Tale id is empty guid");
        if (Guid.Empty == taleVersionId) throw new GrainIdException("<IAnecdoteContainerGrain> Tale version id is empty guid");
        return $"{taleVersionId}\\AnecdoteContainer";
    }

    public static string FetchPersonContainer(Guid taleId, Guid taleVersionId)
    {
        if (Guid.Empty == taleId) throw new GrainIdException("<IPersonContainerGrain> Tale id is empty guid");
        if (Guid.Empty == taleVersionId) throw new GrainIdException("<IPersonContainerGrain> Tale version id is empty guid");
        return $"{taleVersionId}\\PersonContainer";
    }

    public static string FetchWorldContainer(Guid taleId, Guid taleVersionId)
    {
        if (Guid.Empty == taleId) throw new GrainIdException("<IWorldContainerGrain> Tale id is empty guid");
        if (Guid.Empty == taleVersionId) throw new GrainIdException("<IWorldContainerGrain> Tale version id is empty guid");
        return $"{taleVersionId}\\WorldContainer";
    }

    // --

    public static string FetchActorExtension(Guid taleId, Guid taleVersionId, string type, string extensionId)
    {
        if (Guid.Empty == taleId) throw new GrainIdException("<IActorExtensionGrain> Tale id is empty guid");
        if (Guid.Empty == taleVersionId) throw new GrainIdException("<IActorExtensionGrain> Tale version id is empty guid");
        if (string.IsNullOrEmpty(extensionId)) throw new GrainIdException("<IActorExtensionGrain> Extension id is empty string or null");
        if (string.IsNullOrEmpty(type)) throw new GrainIdException("<IActorExtensionGrain> Extension type is empty string or null");
        return $"{taleVersionId}\\!{type}:{extensionId}";
    }

    public static string FetchPersonExtension(Guid taleId, Guid taleVersionId, string type, string extensionId)
    {
        if (Guid.Empty == taleId) throw new GrainIdException("<IPersonExtensionGrain> Tale id is empty guid");
        if (Guid.Empty == taleVersionId) throw new GrainIdException("<IPersonExtensionGrain> Tale version id is empty guid");
        if (string.IsNullOrEmpty(extensionId)) throw new GrainIdException("<IPersonExtensionGrain> Extension id is empty string or null");
        if (string.IsNullOrEmpty(type)) throw new GrainIdException("<IPersonExtensionGrain> Extension type is empty string or null");
        return $"{taleVersionId}\\!{type}:{extensionId}";
    }

    public static string FetchAnecdoteExtension(Guid taleId, Guid taleVersionId, string type, string extensionId)
    {
        if (Guid.Empty == taleId) throw new GrainIdException("<IAnecdoteExtensionGrain> Tale id is empty guid");
        if (Guid.Empty == taleVersionId) throw new GrainIdException("<IAnecdoteExtensionGrain> Tale version id is empty guid");
        if (string.IsNullOrEmpty(extensionId)) throw new GrainIdException("<IAnecdoteExtensionGrain> Extension id is empty string or null");
        if (string.IsNullOrEmpty(type)) throw new GrainIdException("<IAnecdoteExtensionGrain> Extension type is empty string or null");
        return $"{taleVersionId}\\!{type}:{extensionId}";
    }

    // --

    public static string FetchActor(Guid taleId, Guid taleVersionId, string target)
    {
        if (Guid.Empty == taleId) throw new GrainIdException("<IActorGrain> Tale id is empty guid");
        if (Guid.Empty == taleVersionId) throw new GrainIdException("<IActorGrain> Tale version id is empty guid");
        if (string.IsNullOrEmpty(target)) throw new GrainIdException("<IActorGrain> Target is empty string or null");
        return $"{taleVersionId}\\ACTOR:{target}";
    }

    public static string FetchEquipment(Guid taleId, Guid taleVersionId, string target)
    {
        if (Guid.Empty == taleId) throw new GrainIdException("<IEquipmentGrain> Tale id is empty guid");
        if (Guid.Empty == taleVersionId) throw new GrainIdException("<IEquipmentGrain> Tale version id is empty guid");
        if (string.IsNullOrEmpty(target)) throw new GrainIdException("<IEquipmentGrain> Target is empty string or null");
        return $"{taleVersionId}\\EQUIPMENT:{target}";
    }

    public static string FetchCohort(Guid taleId, Guid taleVersionId, string target)
    {
        if (Guid.Empty == taleId) throw new GrainIdException("<ICohortGrain> Tale id is empty guid");
        if (Guid.Empty == taleVersionId) throw new GrainIdException("<ICohortGrain> Tale version id is empty guid");
        if (string.IsNullOrEmpty(target)) throw new GrainIdException("<ICohortGrain> Target is empty string or null");
        return $"{taleVersionId}\\COHORT:{target}";
    }

    public static string FetchAnecdote(Guid taleId, Guid taleVersionId, string target)
    {
        if (Guid.Empty == taleId) throw new GrainIdException("<IAnecdoteGrain> Tale id is empty guid");
        if (Guid.Empty == taleVersionId) throw new GrainIdException("<IAnecdoteGrain> Tale version id is empty guid");
        if (string.IsNullOrEmpty(target)) throw new GrainIdException("<IAnecdoteGrain> Target is empty string or null");
        return $"{taleVersionId}\\ANECDOTE:{target}";
    }

    public static string FetchPerson(Guid taleId, Guid taleVersionId, string target)
    {
        if (Guid.Empty == taleId) throw new GrainIdException("<IPersonGrain> Tale id is empty guid");
        if (Guid.Empty == taleVersionId) throw new GrainIdException("<IPersonGrain> Tale version id is empty guid");
        if (string.IsNullOrEmpty(target)) throw new GrainIdException("<IPersonGrain> Target is empty string or null");
        return $"{taleVersionId}\\PERSON:{target}";
    }

    public static string FetchSettlement(Guid taleId, Guid taleVersionId, string target)
    {
        if (Guid.Empty == taleId) throw new GrainIdException("<ISettlementGrain> Tale id is empty guid");
        if (Guid.Empty == taleVersionId) throw new GrainIdException("<ISettlementGrain> Tale version id is empty guid");
        if (string.IsNullOrEmpty(target)) throw new GrainIdException("<ISettlementGrain> Target is empty string or null");
        return $"{taleVersionId}\\SETTLEMENT:{target}";
    }

    public static string FetchRace(Guid taleId, Guid taleVersionId, string target)
    {
        if (Guid.Empty == taleId) throw new GrainIdException("<IRaceGrain> Tale id is empty guid");
        if (Guid.Empty == taleVersionId) throw new GrainIdException("<IRaceGrain> Tale version id is empty guid");
        if (string.IsNullOrEmpty(target)) throw new GrainIdException("<IRaceGrain> Target is empty string or null");
        return $"{taleVersionId}\\RACE:{target}";
    }

    public static string FetchGroup(Guid taleId, Guid taleVersionId, string target)
    {
        if (Guid.Empty == taleId) throw new GrainIdException("<IGroupGrain> Tale id is empty guid");
        if (Guid.Empty == taleVersionId) throw new GrainIdException("<IGroupGrain> Tale version id is empty guid");
        if (string.IsNullOrEmpty(target)) throw new GrainIdException("<IGroupGrain> Target is empty string or null");
        return $"{taleVersionId}\\GROUP:{target}";
    }

    public static string FetchCache(Guid taleId, Guid taleVersionId, string target)
    {
        if (Guid.Empty == taleId) throw new GrainIdException("<ICacheGrain> Tale id is empty guid");
        if (Guid.Empty == taleVersionId) throw new GrainIdException("<ICacheGrain> Tale version id is empty guid");
        if (string.IsNullOrEmpty(target)) throw new GrainIdException("<ICacheGrain> Target is empty string or null");
        return $"{taleVersionId}\\CACHE:{target}";
    }

    public static string FetchFaction(Guid taleId, Guid taleVersionId, string target)
    {
        if (Guid.Empty == taleId) throw new GrainIdException("<IFactionGrain> Tale id is empty guid");
        if (Guid.Empty == taleVersionId) throw new GrainIdException("<IFactionGrain> Tale version id is empty guid");
        if (string.IsNullOrEmpty(target)) throw new GrainIdException("<IFactionGrain> Target is empty string or null");
        return $"{taleVersionId}\\FACTION:{target}";
    }

    public static string FetchWorld(Guid taleId, Guid taleVersionId)
    {
        if (Guid.Empty == taleId) throw new GrainIdException("<IWorldGrain> Tale id is empty guid");
        if (Guid.Empty == taleVersionId) throw new GrainIdException("<IWorldGrain> Tale version id is empty guid");
        return $"{taleVersionId}\\WORLD";
    }
}
