$ItemPersistance::SaveInterval = 60000 * 5; //five minutes
$ItemPersistance::FileLocation = "config/server/ItemPersistance/";

$ItemPersistance::ItemSet = new SimSet();
$ItemPersistance::SaveSchedule = 0;

//remove item popping
function Item::schedulePop(%obj){}

function ItemPersistanceLoad()
{
    //create the file object for reading
    %in = new fileObject();
    %success = %in.openForRead($ItemPersistance::FileLocation @ "items.txt");

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
        warn("Item Persistance: Failed to open item file for loading");
    }

    %in.close();
    %in.delete();

    //after the file is finished loading start the save schedule
    ItemPersistanceSaveSchedule();
}

function ItemPersistanceSaveSchedule()
{
    cancel($ItemPersistance::SaveSchedule);
    
    //create the file object for writing
    %out = new fileObject();
    %success = %out.openForWrite($ItemPersistance::FileLocation @ "items.txt");

    if(%success)
    {
        %set = $ItemPersistance::ItemSet;
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
        warn("Item Persistance: Failed to open item file for saving");
    }

    %out.close();
    %out.delete();

    $ItemPersistance::SaveSchedule = schedule($ItemPersistance::SaveInterval,$ItemPersistance::ItemSet,"ItemPersistanceSaveSchedule");
}

package item_persistance
{
    function ItemData::OnAdd(%db,%obj)
    {
        //new item created
        //make sure to add it to a simset for easy keeping
        $ItemPersistance::ItemSet.add(%obj);

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
activatePackage(item_persistance);

ItemPersistanceLoad();