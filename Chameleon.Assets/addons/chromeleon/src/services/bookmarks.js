import { log } from "./logger.js";

/**
 * Background script for a Chrome extension to add URLs as bookmarks in a folder
 *
 * To use in your extension:
 * 1. Add this as a background script in your manifest.json:
 *    "background": {
 *      "service_worker": "background.js"
 *    }
 * 2. Add the bookmarks permission in manifest.json:
 *    "permissions": ["bookmarks"]
 */

/**
 * Validates a URL string and attempts to fix it by adding http:// if needed
 * @param {string|object} item - URL string or object with url property
 * @returns {object} - Object with title and url properties
 */
function normalizeUrl(item) {
  let url, title;
  
  if (typeof item === 'string') {
    url = item;
    title = item;
  } else {
    url = item.url;
    title = item.title || item.url;
  }

  try {
    if (!/^https?:\/\//i.test(url)) {
      url = "http://" + url;
    }
    const urlObj = new URL(url);
    return { title, url: urlObj.href };
  } catch (error) {
    log.warn(`Invalid URL: ${url} - ${error.message}`);
    return { title: url, url };
  }
}

/**
 * Gets the Bookmarks Bar ID
 * @returns {Promise<string>} - Bookmarks Bar folder ID
 */
async function getBookmarksBarId() {
  const bookmarksTree = await chrome.bookmarks.getTree();
  return bookmarksTree[0].children[0].id;
}

/**
 * Finds or creates a bookmark folder
 * @param {string} folderName - Name of the folder
 * @param {string} [parentId] - Optional parent folder ID
 * @returns {Promise<object>} - Bookmark folder object
 */
async function ensureBookmarkFolder(folderName, parentId = null) {
  const results = await chrome.bookmarks.search({ title: folderName });
  
  // Find existing folder
  for (const bookmark of results) {
    const [node] = await chrome.bookmarks.get(bookmark.id);
    if (!node.url && (!parentId || node.parentId === parentId)) {
      return node;
    }
  }

  // Create new folder
  if (!parentId) {
    parentId = await getBookmarksBarId();
  }
  
  return chrome.bookmarks.create({
    parentId,
    title: folderName,
  });
}

/**
 * Gets existing bookmarks in a folder as a normalized URL map
 * @param {string} folderId - ID of the folder
 * @returns {Promise<Map>} - Map of normalized URLs to bookmark objects
 */
async function getExistingBookmarks(folderId) {
  const children = await chrome.bookmarks.getChildren(folderId);
  const bookmarkMap = new Map();
  
  for (const child of children) {
    if (child.url) {
      const normalizedUrl = child.url.toLowerCase().replace(/\/$/, "");
      bookmarkMap.set(normalizedUrl, child);
    }
  }
  
  return bookmarkMap;
}

/**
 * Adds a list of URLs as bookmarks in a folder
 * @param {string} folderName - Name of the folder to add bookmarks to
 * @param {Array<{url: string, title: string}|string>} urlList - List of URL objects or strings
 * @param {string} [parentId] - Optional parent folder ID
 * @returns {Promise<object>} - Result object with success status and created bookmarks
 */
export async function addUrlsAsBookmarks(folderName, urlList, parentId = null) {
  try {
    log.info(`Starting to add bookmarks to folder: ${folderName}`);
    log.debug(`URL list contains ${urlList.length} items`);
    
    const folder = await ensureBookmarkFolder(folderName, parentId);
    log.info(`Using folder: ${folder.title} (ID: ${folder.id})`);

    const existingBookmarks = await getExistingBookmarks(folder.id);
    
    const stats = { processed: 0, added: 0, updated: 0, skipped: 0 };
    const operations = [];

    // Prepare operations
    for (const item of urlList) {
      stats.processed++;
      log.debug(`Processing item ${stats.processed}/${urlList.length}: ${JSON.stringify(item)}`);
      
      const { title, url } = normalizeUrl(item);
      const normalizedUrl = url.toLowerCase().replace(/\/$/, "");
      const existing = existingBookmarks.get(normalizedUrl);

      if (existing?.title !== title) {
        if (existing) {
          // Update operation
          operations.push({
            type: 'update',
            id: existing.id,
            title,
            originalTitle: existing.title,
            url
          });
          stats.updated++;
        } else {
          // Create operation
          operations.push({
            type: 'create',
            parentId: folder.id,
            title,
            url
          });
          stats.added++;
        }
      } else {
        log.debug(`Bookmark already exists with same title: ${url}`);
        stats.skipped++;
      }
    }

    // Execute operations in parallel batches
    const batchSize = 10;
    for (let i = 0; i < operations.length; i += batchSize) {
      const batch = operations.slice(i, i + batchSize);
      const promises = batch.map(async (op) => {
        try {
          if (op.type === 'update') {
            log.info(`Updating bookmark title from "${op.originalTitle}" to "${op.title}" for ${op.url}`);
            await chrome.bookmarks.update(op.id, { title: op.title });
          } else {
            log.info(`Creating new bookmark: ${op.title} - ${op.url}`);
            await chrome.bookmarks.create({
              parentId: op.parentId,
              title: op.title,
              url: op.url,
            });
          }
        } catch (err) {
          log.error(`Error ${op.type}ing bookmark for ${op.url}: ${err.message}`);
          if (op.type === 'update') stats.updated--;
          else stats.added--;
          stats.skipped++;
        }
      });
      
      await Promise.all(promises);
    }

    const result = {
      success: true,
      folderId: folder.id,
      folderName: folder.title,
      ...stats
    };

    log.info(`Bookmark operation completed: ${JSON.stringify(result)}`);
    return result;
  } catch (error) {
    log.error(`Error adding bookmarks: ${error.message}`);
    return {
      success: false,
      message: `Error adding bookmarks: ${error.message}`,
    };
  }
}
