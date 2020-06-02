using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
                //return await db.Sequence.Find(_ => true).ToListAsync();
                return await db.Contact.AsQueryable().ToListAsync();
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
                return await db.Delivery.AsQueryable().ToListAsync();
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
