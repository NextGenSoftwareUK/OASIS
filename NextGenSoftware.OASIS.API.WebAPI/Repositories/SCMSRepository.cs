using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using MongoDB.Bson;

namespace NextGenSoftware.OASIS.API.WebAPI
{
    public class SCMSRepository : ISCMSRepository
    {
        MongoDbContext db = new MongoDbContext();
        public async Task Add(User User)
        {
            try
            {
                await db.User.InsertOneAsync(User);
            }
            catch
            {
                throw;
            }
        }
        public async Task<Sequence> GetSequence(string id)
        {
            try
            {
                FilterDefinition<Sequence> filter = Builders<Sequence>.Filter.Eq("Id", id);
                return await db.Sequence.Find(filter).FirstOrDefaultAsync();
            }
            catch
            {
                throw;
            }
        }

        public async Task<IEnumerable<Sequence>> GetAllSequences()
        {
            try
            {
                //return await db.Sequence.Find(_ => true).ToListAsync();
                return await db.Sequence.AsQueryable().ToListAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<Phase> GetPhase(string id)
        {
            try
            {
                FilterDefinition<Phase> filter = Builders<Phase>.Filter.Eq("Id", id);
                return await db.Phase.Find(filter).FirstOrDefaultAsync();
            }
            catch
            {
                throw;
            }
        }

        public async Task<IEnumerable<Phase>> GetAllPhases()
        {
            try
            {
                //return await db.Sequence.Find(_ => true).ToListAsync();
                return await db.Phase.AsQueryable().ToListAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<Contract> GetContract(string id)
        {
            try
            {
                FilterDefinition<Contract> filter = Builders<Contract>.Filter.Eq("Id", id);
                return await db.Contract.Find(filter).FirstOrDefaultAsync();
            }
            catch
            {
                throw;
            }
        }

        public async Task<IEnumerable<Contract>> GetAllContracts()
        {
            try
            {
                //return await db.Sequence.Find(_ => true).ToListAsync();
                return await db.Contract.AsQueryable().ToListAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<Contact> GetContact(string id)
        {
            try
            {
                FilterDefinition<Contact> filter = Builders<Contact>.Filter.Eq("Id", id);
                return await db.Contact.Find(filter).FirstOrDefaultAsync();
            }
            catch
            {
                throw;
            }
        }

        public async Task<IEnumerable<Contact>> GetAllContacts()
        {
            try
            {
                var contacts = db.Contact.AsQueryable().ToListAsync();
                var users = db.User.AsQueryable().ToListAsync();

                foreach (Contact contact in contacts.Result)
                {
                    foreach (User user in users.Result)
                    { 
                        if (contact.UserId == user.Id)
                        {
                            contact.FirstName = user.FirstName;
                            contact.LastName = user.LastName;
                            contact.Address = user.Address;
                            contact.Country = user.Country;
                            contact.County = user.County;
                            contact.CreatedByUserId = user.CreatedByUserId;
                            contact.CreatedDate = user.CreatedDate;
                            contact.DeletedByUserId = user.DeletedByUserId;
                            contact.DeletedDate = user.DeletedDate;
                            contact.DOB = user.DOB;
                            contact.Landline = user.LastName;
                            contact.Mobile = user.Mobile;
                            contact.ModifledByUserId = user.ModifledByUserId;
                            contact.ModifledDate = user.ModifledDate;
                            contact.Password = user.Password;
                            contact.Postcode = user.Postcode;
                            contact.Title = user.Title;
                            contact.Town = user.Town;
                            contact.Username = user.Username;
                            contact.UserType = user.UserType;
                            contact.Version = user.Version;
                            break;
                        }
                    }
                }

                //return await db.Sequence.Find(_ => true).ToListAsync();
                //return await db.Contact.AsQueryable().ToListAsync();
                return contacts.Result;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        
        
        public async Task<IEnumerable<Contact>> GetAllContacts(int SequenceNo, int PhaseNo)
        {
            try
            {
                FilterDefinition<Sequence> sequenceFilter = Builders<Sequence>.Filter.Eq("SequenceNo", SequenceNo);
                var filteredSequences = await db.Sequence.Find(sequenceFilter).ToListAsync();

                //var contacts = await db.Contact.Find({"VIP": true, "Country": "Germany"});
                List<Contact> filteredContacts = new List<Contact>();
                //FilterDefinition<Contact> filter = Builders<Contact>.Filter.Eq("SequenceNo", SequenceNo);

            //    var contacts = await db.Contact.Find(filter).ToListAsync();
                

          

              //  string connectionString = "mongodb://localhost:27017";
              //  var client = new MongoClient(connectionString);

              //  var db = client.GetDatabase("test");
                var contacts = db.MongoDbBEB.GetCollection<Contact>("Contact");
                var resultOfJoin = contacts.Aggregate()
                    .Lookup("Phase", "PhaseId", "_id", @as: "Phase")
                    //.Lookup("Phase.Sequence", "Phase.SequenceId", "_id", @as: "Sequence")  //TODO: Need to find out why this doesn't work?!
                   .Unwind("Phase")
                    //.Unwind("Sequence")
                    .As<Contact>()
                    .ToList();

     
                foreach (Contact contact in resultOfJoin)
                {
                    if (contact.Phase.PhaseNo == PhaseNo)
                    {
                        foreach (Sequence sequence in filteredSequences)
                        {
                            if (contact.Phase.SequenceId == sequence.Id)
                            {
                                filteredContacts.Add(contact);
                                break;
                            }
                        }
                    }

                    //if (contact.Phase.PhaseNo == PhaseNo && contact.Phase.Sequence.SequenceNo == SequenceNo.ToString())
                    //    filteredContacts.Add(contact);
                }

                // var contacts = db.Contact.AsQueryable().ToListAsync();
                var users = db.User.AsQueryable().ToListAsync();

                //foreach (Contact contact in contacts.Result)
                foreach (Contact contact in filteredContacts)
                {
                    foreach (User user in users.Result)
                    {
                        if (contact.UserId == user.Id)
                        {
                            contact.FirstName = user.FirstName;
                            contact.LastName = user.LastName;
                            contact.Address = user.Address;
                            contact.Country = user.Country;
                            contact.County = user.County;
                            contact.CreatedByUserId = user.CreatedByUserId;
                            contact.CreatedDate = user.CreatedDate;
                            contact.DeletedByUserId = user.DeletedByUserId;
                            contact.DeletedDate = user.DeletedDate;
                            contact.Email = user.Email;
                            contact.DOB = user.DOB;
                            contact.Landline = user.LastName;
                            contact.Mobile = user.Mobile;
                            contact.ModifledByUserId = user.ModifledByUserId;
                            contact.ModifledDate = user.ModifledDate;
                            contact.Password = user.Password;
                            contact.Postcode = user.Postcode;
                            contact.Title = user.Title;
                            contact.Town = user.Town;
                            contact.Username = user.Username;
                            contact.UserType = user.UserType;
                            contact.Version = user.Version;
                            break;
                        }
                    }
                }

                //return await db.Sequence.Find(_ => true).ToListAsync();
                //return await db.Contact.AsQueryable().ToListAsync();
                //return contacts.Result;
                return filteredContacts;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        

        public async Task Update(User User)
        {
            try
            {
                await db.User.ReplaceOneAsync(filter: g => g.Id == User.Id, replacement: User);
            }
            catch
            {
                throw;
            }
        }
        public async Task Delete(string id)
        {
            try
            {
                FilterDefinition<User> data = Builders<User>.Filter.Eq("Id", id);
                await db.User.DeleteOneAsync(data);
            }
            catch
            {
                throw;
            }
        }

        public Task AddSequence(Sequence sequence)
        {
            throw new System.NotImplementedException();
        }

        public Task Update(Sequence sequence)
        {
            throw new System.NotImplementedException();
        }

        public async Task<Delivery> GetDelivery(string id)
        {
            try
            {
                FilterDefinition<Delivery> filter = Builders<Delivery>.Filter.Eq("Id", id);
                return await db.Delivery.Find(filter).FirstOrDefaultAsync();
            }
            catch
            {
                throw;
            }
        }

        public async Task<IEnumerable<Delivery>> GetAllDeliveries()
        {
            try
            {
                // await db.Delivery.AsQueryable().ToListAsync();
                var deliveries = db.Delivery.AsQueryable().ToListAsync();
                var deliveryItems = db.DeliveryItem.AsQueryable().ToListAsync();

                foreach (Delivery delivery in deliveries.Result)
                {
                    delivery.DeliveryItems = new List<DeliveryItem>();

                    foreach (DeliveryItem deliveryItem in deliveryItems.Result)
                    {
                        if (deliveryItem.DeliveryId == delivery.Id)
                        {
                            delivery.DeliveryItems.Add(deliveryItem);

                            FilterDefinition<Material> filter = Builders<Material>.Filter.Eq("Id", deliveryItem.MaterialId);
                            Material material = await db.Material.Find(filter).FirstOrDefaultAsync();

                            if (material != null)
                            {
                                deliveryItem.Material = material;

                                FilterDefinition<File> fileFilter = Builders<File>.Filter.Eq("Id", material.FileId);
                                File file = await db.File.Find(fileFilter).FirstOrDefaultAsync();
                                material.File = file;
                            }
                        }
                    }
                }

                return deliveries.Result;

                // var deliveries = db.GetCollection<Delivery>("Instances");


                //var resultOfJoin = deliveries.Result.Aggregate()
                //    .Lookup("Templates", "TemplateId", "_id", @as: "Template")
                //    .Unwind("Template")
                //    .As<Instance>()
                //    .ToList();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<DeliveryItem> GetDeliveryItem(string id)
        {
            try
            {
                FilterDefinition<DeliveryItem> filter = Builders<DeliveryItem>.Filter.Eq("Id", id);
                return await db.DeliveryItem.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<IEnumerable<DeliveryItem>> GetAllDeliveryItems()
        {
            try
            {
                return await db.DeliveryItem.AsQueryable().ToListAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<Drawing> GetDrawing(string id)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<Drawing>> GetAllDrawings()
        {
            try
            {
                return await db.Drawing.AsQueryable().ToListAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<IEnumerable<Drawing>> GetAllDrawings(int SequenceNo, int PhaseNo, bool includePhaseObject = false, bool includeFileObject = true)
        {
            try
            {
                List<Drawing> filteredDrawings = new List<Drawing>();

                FilterDefinition<Phase> phaseFilter = Builders<Phase>.Filter.Eq("PhaseNo", PhaseNo);
                var filteredPhases = await db.Phase.Find(phaseFilter).ToListAsync();

                FilterDefinition<Sequence> sequenceFilter = Builders<Sequence>.Filter.Eq("SequenceNo", SequenceNo);
                var filteredSequences = await db.Sequence.Find(sequenceFilter).ToListAsync();

                var drawings = db.MongoDbBEB.GetCollection<Drawing>("Drawing");

                foreach (Drawing drawing in drawings.AsQueryable())
                {
                    foreach (Phase phase in filteredPhases)
                    {
                        if (phase.Id == drawing.PhaseId)
                        {
                            if (includePhaseObject)
                                drawing.Phase = phase;

                            foreach (Sequence sequence in filteredSequences)
                            {
                                if (phase.SequenceId == sequence.Id)
                                {
                                    filteredDrawings.Add(drawing);
                                    break;
                                }
                            }
                        }
                    }
                }

                // Load the child File objects.
                if (includeFileObject)
                {
                    foreach (Drawing drawing in filteredDrawings)
                        drawing.File = await GetFile(drawing.FileId);
                }


                //Use to pull back the whole Phase object too but that adds a lot more to the payload send back so we only want the File object
                /*
                var drawings = db.MongoDbBEB.GetCollection<Drawing>("Drawing");

                var resultOfJoin = drawings.Aggregate();

                if (includeFileObject)
                    resultOfJoin = (IAggregateFluent<Drawing>)resultOfJoin.Lookup("File", "FileId", "_id", @as: "File").Unwind("File");

                if (includePhaseObject)
                    resultOfJoin = (IAggregateFluent<Drawing>)resultOfJoin.Lookup("Phase", "PhaseId", "_id", @as: "Phase").Unwind("Phase");
                
                var drawingsExtended = resultOfJoin.As<Drawing>().ToList();
                */

                /*
                var resultOfJoin = drawings.Aggregate()
                   // .Lookup("Phase", "PhaseId", "_id", @as: "Phase")
                    .Lookup("File", "FileId", "_id", @as: "File")
                   //.Lookup("Phase.Sequence", "Phase.SequenceId", "_id", @as: "Sequence")  //TODO: Need to find out why this doesn't work?!
                 //  .Unwind("Phase")
                   .Unwind("File")
                    //.Unwind("Sequence")
                    .As<Drawing>()
                    .ToList();
                    */

                /*
                foreach (Drawing drawing in resultOfJoin)
                {
                    if (drawing.Phase.PhaseNo == PhaseNo)
                    {
                        foreach (Sequence sequence in filteredSequences)
                        {
                            if (drawing.Phase.SequenceId == sequence.Id)
                            {
                                filteredDrawings.Add(drawing);
                                break;
                            }
                        }
                    }

                    //if (contact.Phase.PhaseNo == PhaseNo && contact.Phase.Sequence.SequenceNo == SequenceNo.ToString())
                    //    filteredContacts.Add(contact);
                }
                
                if (!includePhaseObject)
                {
                    FilterDefinition<Phase> phaseFilter = Builders<Phase>.Filter.Eq("PhaseNo", PhaseNo);
                    var filteredPhases = await db.Phase.Find(phaseFilter).ToListAsync();

                    foreach (Drawing drawing in drawingsExtended)
                    {
                        foreach (Phase phase in filteredPhases)
                        {
                            if (phase.Id == drawing.PhaseId)
                            {
                                foreach (Sequence sequence in filteredSequences)
                                {
                                    if (phase.SequenceId == sequence.Id)
                                    {
                                        filteredDrawings.Add(drawing);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    //If Phase has been loaded already (later we will try and get the join above to also load sequences)
                    foreach (Drawing drawing in drawingsExtended)
                    {
                        if (drawing.Phase.PhaseNo == PhaseNo)
                        {
                            foreach (Sequence sequence in filteredSequences)
                            {
                                if (drawing.Phase.SequenceId == sequence.Id)
                                {
                                    filteredDrawings.Add(drawing);
                                    break;
                                }
                            }
                        }   
                    }
                }
                */

                // Load the child File objects.
                //foreach (Drawing drawing in filteredDrawings)
                //  drawing.File = await GetFile(drawing.FileId);

                return filteredDrawings;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<File> GetFile(string id)
        {
            try
            {
                FilterDefinition<File> filter = Builders<File>.Filter.Eq("Id", id);
                return await db.File.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<IEnumerable<File>> GetAllFiles()
        {
            try
            {
                return await db.File.AsQueryable().ToListAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<Handover> GetHandover(string id)
        {
            try
            {
                FilterDefinition<Handover> filter = Builders<Handover>.Filter.Eq("Id", id);
                return await db.Handover.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<IEnumerable<Handover>> GetAllHandovers()
        {
            try
            {
                return await db.Handover.AsQueryable().ToListAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<Link> GetLink(string id)
        {
            try
            {
                FilterDefinition<Link> filter = Builders<Link>.Filter.Eq("Id", id);
                return await db.Link.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<IEnumerable<Link>> GetAllLinks()
        {
            try
            {
                return await db.Link.AsQueryable().ToListAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<Log> GetLog(string id)
        {
            try
            {
                FilterDefinition<Log> filter = Builders<Log>.Filter.Eq("Id", id);
                return await db.Log.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<IEnumerable<Log>> GetAllLogs()
        {
            try
            {
                return await db.Log.AsQueryable().ToListAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<Material> GetMaterial(string id)
        {
            try
            {
                FilterDefinition<Material> filter = Builders<Material>.Filter.Eq("Id", id);
                return await db.Material.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<IEnumerable<Material>> GetAllMaterials()
        {
            try
            {
                return await db.Material.AsQueryable().ToListAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<Note> GetNote(string id)
        {
            try
            {
                FilterDefinition<Note> filter = Builders<Note>.Filter.Eq("Id", id);
                return await db.Note.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<IEnumerable<Note>> GetAllNotes()
        {
            try
            {
                return await db.Note.AsQueryable().ToListAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<Trigger> GetTrigger(string id)
        {
            try
            {
                FilterDefinition<Trigger> filter = Builders<Trigger>.Filter.Eq("Id", id);
                return await db.Trigger.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<IEnumerable<Trigger>> GetAllTriggers()
        {
            try
            {
                return await db.Trigger.AsQueryable().ToListAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
