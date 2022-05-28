$ItemPersistence::SaveInterval = 60000 * 5; //five minutes
$ItemPersistence::FileLocation = "config/server/ItemPersistence/";

$ItemPersistence::ItemSet = new SimSet();
$ItemPersistence::SaveSchedule = 0;

//remove item popping
function Item::schedulePop(%obj){}

function ItemPersistenceLoad()
{
    //create the file object for reading
    %in = new fileObject();
    %success = %in.openForRead($ItemPersistence::FileLocation @ "items.txt");

    if(%success)
    {
        while(!%in.isEOF())
        {
            //datablock name TAB transform
            %line = %in.readLine();
            %dbName = getField(%line,0);
            %transform = getField(%line,1);

            new Item()
            {
                dataBlock = %dbName; 
                static = true; 
                LoadedPersistantItem = true;
            }
            .setTransform(%transform);
        }
    }
    else
    {
        warn("Item Persistence: Failed to open item file for loading");
    }

    %in.close();
    %in.delete();

    //after the file is finished loading start the save schedule
    ItemPersistenceSaveSchedule();
}

function ItemPersistenceSaveSchedule()
{
    cancel($ItemPersistence::SaveSchedule);
    
    //create the file object for writing
    %out = new fileObject();
    %success = %out.openForWrite($ItemPersistence::FileLocation @ "items.txt");

    if(%success)
    {
        %set = $ItemPersistence::ItemSet;
        %count = %set.getCount();
        for(%i = 0; %i < %count; %i++)
        {
            //datablock name TAB transform
            %item = %set.getObject(%i);
            %dbName = %item.getDatablock().getName();
            %transform = %item.getTransform();

            %out.writeLine(%dbName TAB %transform);
        }
    }
    else
    {
        warn("Item Persistence: Failed to open item file for saving");
    }

    %out.close();
    %out.delete();

    $ItemPersistence::SaveSchedule = schedule($ItemPersistence::SaveInterval,$ItemPersistence::ItemSet,"ItemPersistenceSaveSchedule");
}

package item_persistence
{
    function miniGameCanUse(%player, %thing)
    {
        //fixes minigame players not being able to pick up these items
        if(%thing.static && %thing.LoadedPersistantItem)
        {
            return 1;
        }

        parent::miniGameCanUse(%player, %thing);
    }

    function ItemData::OnAdd(%db,%obj)
    {
        //new item created
        //make sure to add it to a simset for easy keeping

        //DO NOT SAVE ITEMS SPAWNS!!
        if(!%obj.static || %obj.LoadedPersistantItem)
        {
            $ItemPersistence::ItemSet.add(%obj);
        }
        

        return Parent::OnAdd(%db,%obj);
    }

    function Item::Respawn(%obj)
    {
        //prevents loaded static items from being "respawned" as an item giver would
        if(%obj.LoadedPersistantItem)
        {
            %obj.delete();
        }
        else
        {
            Parent::Respawn(%obj);
        }
    }
};
activatePackage(item_persistence);

ItemPersistenceLoad();